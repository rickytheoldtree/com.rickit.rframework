using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;

namespace RicKit.RFramework.UIComponents.EnhancedScroller
{
    public delegate void CellViewVisibilityChangedDelegate(EnhancedScrollerCellView cellView);

    public delegate void CellViewWillRecycleDelegate(EnhancedScrollerCellView cellView);

    public delegate void ScrollerScrolledDelegate(EnhancedScroller scroller, Vector2 val, float scrollPosition);

    public delegate void ScrollerScrollingChangedDelegate(EnhancedScroller scroller, bool scrolling);

    public delegate void ScrollerTweeningChangedDelegate(EnhancedScroller scroller, bool tweening);

    public delegate void CellViewInstantiated(EnhancedScroller scroller, EnhancedScrollerCellView cellView);

    public delegate void CellViewReused(EnhancedScroller scroller, EnhancedScrollerCellView cellView);

    public delegate float CustomTweenFunction(float start, float end, float remainingTimePercentage);

    [RequireComponent(typeof(ScrollRect))]
    public class EnhancedScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        #region Public

        public enum ScrollDirectionEnum
        {
            Vertical,
            Horizontal
        }

        public enum CellViewPositionEnum
        {
            Before,
            After
        }

        public enum ScrollbarVisibilityEnum
        {
            OnlyIfNeeded,
            Always,
            Never
        }

        public ScrollDirectionEnum scrollDirection;
        public float spacing;
        public RectOffset padding;
        [SerializeField] private bool loop;
        public bool loopWhileDragging = true;
        public float maxVelocity;
        [SerializeField] private ScrollbarVisibilityEnum scrollbarVisibility;
        public bool interruptTweeningOnDrag;
        public bool interruptTweeningOnPointerDown;
        public bool tweenPaused;
        private float lookAheadBefore;

        public float LookAheadBefore
        {
            get => lookAheadBefore;
            set => lookAheadBefore = Mathf.Abs(value);
        }

        private float lookAheadAfter;

        public float LookAheadAfter
        {
            get => lookAheadAfter;
            set => lookAheadAfter = Mathf.Abs(value);
        }

        public float CalculateStartCellBias { get; set; }

        public event CellViewVisibilityChangedDelegate CellViewVisibilityChanged;
        public event CellViewWillRecycleDelegate CellViewWillRecycle;
        public event ScrollerScrolledDelegate ScrollerScrolled;
        public event ScrollerScrollingChangedDelegate ScrollerScrollingChanged;
        public event ScrollerTweeningChangedDelegate ScrollerTweeningChanged;
        public event CellViewInstantiated CellViewInstantiated;
        public event CellViewReused CellViewReused;
        public event CustomTweenFunction CustomTweenFunction;

        public IEnhancedScrollerDelegate Delegate
        {
            get => mDelegate;
            set
            {
                mDelegate = value;
                reloadData = true;
            }
        }

        public float ScrollPosition
        {
            get => scrollPosition;
            set
            {
                value = Mathf.Clamp(value, 0, ScrollSize);

                if (!Mathf.Approximately(scrollPosition, value))
                {
                    scrollPosition = value;
                    if (scrollDirection == ScrollDirectionEnum.Vertical)
                    {
                        scrollRect.verticalNormalizedPosition = 1f - scrollPosition / ScrollSize;
                    }
                    else
                    {
                        scrollRect.horizontalNormalizedPosition = scrollPosition / ScrollSize;
                    }
                }
            }
        }

        public float ScrollSize
        {
            get
            {
                if (scrollDirection == ScrollDirectionEnum.Vertical)
                    return Mathf.Max(container.rect.height - scrollRectTransform.rect.height, 0);
                return Mathf.Max(container.rect.width - scrollRectTransform.rect.width, 0);
            }
        }

        public float NormalizedScrollPosition => scrollPosition <= 0 ? 0 : scrollPosition / ScrollSize;

        public bool Loop
        {
            get => loop;
            set
            {
                if (loop != value)
                {
                    var originalScrollPosition = scrollPosition;

                    loop = value;

                    Resize(false);

                    if (loop)
                    {
                        ScrollPosition = loopFirstScrollPosition + originalScrollPosition;
                    }
                    else
                    {
                        ScrollPosition = originalScrollPosition - loopFirstScrollPosition;
                    }

                    ScrollbarVisibility = scrollbarVisibility;
                }
            }
        }

        public ScrollbarVisibilityEnum ScrollbarVisibility
        {
            get => scrollbarVisibility;
            set
            {
                scrollbarVisibility = value;

                if (scrollbar)
                {
                    if (cellViewOffsetArray != null && cellViewOffsetArray.Count > 0)
                    {
                        if (scrollDirection == ScrollDirectionEnum.Vertical)
                        {
                            ScrollRect.verticalScrollbar = scrollbar;
                        }
                        else
                        {
                            ScrollRect.horizontalScrollbar = scrollbar;
                        }

                        if (cellViewOffsetArray[^1] < ScrollRectSize || loop)
                        {
                            scrollbar.gameObject.SetActive(scrollbarVisibility == ScrollbarVisibilityEnum.Always);
                        }
                        else
                        {
                            scrollbar.gameObject.SetActive(scrollbarVisibility != ScrollbarVisibilityEnum.Never);
                        }

                        if (!scrollbar.gameObject.activeSelf)
                        {
                            ScrollRect.verticalScrollbar = null;
                            ScrollRect.horizontalScrollbar = null;
                        }
                    }
                }
            }
        }

        public Vector2 Velocity
        {
            get => scrollRect.velocity;
            set => scrollRect.velocity = value;
        }

        public float LinearVelocity
        {
            get => scrollDirection == ScrollDirectionEnum.Vertical ? scrollRect.velocity.y : scrollRect.velocity.x;
            set => scrollRect.velocity = scrollDirection == ScrollDirectionEnum.Vertical
                ? new Vector2(0, value)
                : new Vector2(value, 0);
        }

        public bool IsScrolling { get; private set; }

        public bool IsTweening { get; private set; }

        public bool IsDragging => dragging;

        public int StartCellViewIndex => activeCellViewsStartIndex;

        public int EndCellViewIndex => activeCellViewsEndIndex;

        public int StartDataIndex => activeCellViewsStartIndex % NumberOfCells;

        public int EndDataIndex => activeCellViewsEndIndex % NumberOfCells;

        public int ActiveCellCount => activeCellViews.Count;

        public int NumberOfCells => mDelegate?.GetNumberOfCells(this) ?? 0;

        public ScrollRect ScrollRect => scrollRect;

        public float ScrollRectSize
        {
            get
            {
                if (scrollDirection == ScrollDirectionEnum.Vertical)
                    return scrollRectTransform.rect.height;
                return scrollRectTransform.rect.width;
            }
        }

        public LayoutElement FirstPadding => firstPadding;

        public LayoutElement LastPadding => lastPadding;

        public RectTransform Container => container;

        public EnhancedScrollerCellView GetCellView(EnhancedScrollerCellView cellPrefab)
        {
            // see if there is a view to recycle
            var cellView = GetRecycledCellView(cellPrefab);
            if (!cellView)
            {
                var go = Instantiate(cellPrefab.gameObject);
                cellView = go.GetComponent<EnhancedScrollerCellView>();
                cellView.transform.SetParent(container);
                cellView.transform.localPosition = Vector3.zero;
                cellView.transform.localRotation = Quaternion.identity;

                CellViewInstantiated?.Invoke(this, cellView);
            }
            else
            {
                cellView.gameObject.SetActive(true);

                CellViewReused?.Invoke(this, cellView);
            }

            return cellView;
        }

        public void ReloadData(float scrollPositionFactor = 0)
        {
            reloadData = false;

            RecycleAllCells();

            if (mDelegate != null)
                Resize(false);

            if (!scrollRect || !scrollRectTransform || !container)
            {
                scrollPosition = 0f;
                return;
            }

            scrollPosition = Mathf.Clamp(scrollPositionFactor * ScrollSize, 0, ScrollSize);
            if (scrollDirection == ScrollDirectionEnum.Vertical)
            {
                scrollRect.verticalNormalizedPosition = 1f - scrollPositionFactor;
            }
            else
            {
                scrollRect.horizontalNormalizedPosition = scrollPositionFactor;
            }

            _RefreshActive();
        }

        public void RefreshActiveCellViews()
        {
            foreach (var c in activeCellViews)
            {
                c.RefreshCellView();
            }
        }

        public void ClearAll()
        {
            ClearActive();
            ClearRecycled();
        }

        public void ClearActive()
        {
            foreach (var c in activeCellViews)
            {
                DestroyImmediate(c.gameObject);
            }

            activeCellViews.Clear();
        }

        public void ClearRecycled()
        {
            foreach (var c in recycledCellViews)
            {
                DestroyImmediate(c.gameObject);
            }

            recycledCellViews.Clear();
        }

        public void ToggleLoop()
        {
            Loop = !loop;
        }

        public void IgnoreLoopJump(bool ignore)
        {
            ignoreLoopJump = ignore;
        }

        public void SetScrollPositionImmediately(float scrollPosition)
        {
            ScrollPosition = scrollPosition;
            _RefreshActive();
        }

        public enum LoopJumpDirectionEnum
        {
            Closest,
            Up,
            Down
        }

        public void JumpToDataIndex(int dataIndex,
            float scrollerOffset = 0,
            float cellOffset = 0,
            bool useSpacing = true,
            TweenType tweenType = TweenType.immediate,
            float tweenTime = 0f,
            Action jumpComplete = null,
            LoopJumpDirectionEnum loopJumpDirection = LoopJumpDirectionEnum.Closest,
            bool forceCalculateRange = false
        )
        {
            var cellOffsetPosition = 0f;

            if (cellOffset != 0)
            {
                var cellSize = mDelegate?.GetCellViewSize(this, dataIndex) ?? 0;

                if (useSpacing)
                {
                    cellSize += spacing;

                    if (dataIndex > 0 && dataIndex < NumberOfCells - 1) cellSize += spacing;
                }

                cellOffsetPosition = cellSize * cellOffset;
            }

            if (Mathf.Approximately(scrollerOffset, 1f))
            {
                cellOffsetPosition += padding.bottom;
            }

            var offset = -(scrollerOffset * ScrollRectSize) + cellOffsetPosition;

            var newScrollPosition = 0f;

            if (loop)
            {
                var numberOfCells = NumberOfCells;

                var set1CellViewIndex = loopFirstCellIndex - (numberOfCells - dataIndex);
                var set2CellViewIndex = loopFirstCellIndex + dataIndex;
                var set3CellViewIndex = loopFirstCellIndex + numberOfCells + dataIndex;

                var set1Position = GetScrollPositionForCellViewIndex(set1CellViewIndex, CellViewPositionEnum.Before) +
                                   offset;
                var set2Position = GetScrollPositionForCellViewIndex(set2CellViewIndex, CellViewPositionEnum.Before) +
                                   offset;
                var set3Position = GetScrollPositionForCellViewIndex(set3CellViewIndex, CellViewPositionEnum.Before) +
                                   offset;

                var set1Diff = Mathf.Abs(scrollPosition - set1Position);
                var set2Diff = Mathf.Abs(scrollPosition - set2Position);
                var set3Diff = Mathf.Abs(scrollPosition - set3Position);

                var setOffset = -(scrollerOffset * ScrollRectSize);

                var currentSet = 0;
                var currentCellViewIndex = 0;
                var nextCellViewIndex = 0;

                if (loopJumpDirection == LoopJumpDirectionEnum.Up || loopJumpDirection == LoopJumpDirectionEnum.Down)
                {
                    currentCellViewIndex = GetCellViewIndexAtPosition(scrollPosition - setOffset + 0.0001f);

                    if (currentCellViewIndex < numberOfCells)
                    {
                        currentSet = 1;
                        nextCellViewIndex = dataIndex;
                    }
                    else if (currentCellViewIndex >= numberOfCells && currentCellViewIndex < numberOfCells * 2)
                    {
                        currentSet = 2;
                        nextCellViewIndex = dataIndex + numberOfCells;
                    }
                    else
                    {
                        currentSet = 3;
                        nextCellViewIndex = dataIndex + numberOfCells * 2;
                    }
                }

                switch (loopJumpDirection)
                {
                    case LoopJumpDirectionEnum.Closest:

                        if (set1Diff < set2Diff)
                        {
                            newScrollPosition = set1Diff < set3Diff ? set1Position : set3Position;
                        }
                        else
                        {
                            newScrollPosition = set2Diff < set3Diff ? set2Position : set3Position;
                        }

                        break;

                    case LoopJumpDirectionEnum.Up:

                        if (nextCellViewIndex < currentCellViewIndex)
                        {
                            newScrollPosition = currentSet == 1 ? set1Position :
                                currentSet == 2 ? set2Position : set3Position;
                        }
                        else
                        {
                            if (currentSet == 1 && currentCellViewIndex == dataIndex)
                            {
                                newScrollPosition = set1Position - singleLoopGroupSize;
                            }
                            else
                            {
                                newScrollPosition = currentSet == 1 ? set3Position :
                                    currentSet == 2 ? set1Position : set2Position;
                            }
                        }

                        break;

                    case LoopJumpDirectionEnum.Down:

                        if (nextCellViewIndex > currentCellViewIndex)
                        {
                            newScrollPosition = currentSet == 1 ? set1Position :
                                currentSet == 2 ? set2Position : set3Position;
                        }
                        else
                        {
                            if (currentSet == 3 && currentCellViewIndex == nextCellViewIndex)
                            {
                                newScrollPosition = set3Position + singleLoopGroupSize;
                            }
                            else
                            {
                                newScrollPosition = currentSet == 1 ? set2Position :
                                    currentSet == 2 ? set3Position : set1Position;
                            }
                        }

                        break;
                }

                if (useSpacing)
                {
                    newScrollPosition -= spacing;
                }
            }
            else
            {
                newScrollPosition = GetScrollPositionForDataIndex(dataIndex, CellViewPositionEnum.Before) + offset;

                newScrollPosition = Mathf.Clamp(newScrollPosition - (useSpacing ? spacing : 0), 0, ScrollSize);
            }

            if (Mathf.Approximately(newScrollPosition, scrollPosition))
            {
                jumpComplete?.Invoke();
                return;
            }

            StartCoroutine(TweenPosition(tweenType, tweenTime, ScrollPosition, newScrollPosition, jumpComplete,
                forceCalculateRange));
        }

        public float GetScrollPositionForCellViewIndex(int cellViewIndex, CellViewPositionEnum insertPosition)
        {
            if (NumberOfCells == 0) return 0;
            if (cellViewIndex < 0) cellViewIndex = 0;

            if (cellViewIndex == 0 && insertPosition == CellViewPositionEnum.Before)
            {
                return scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left;
            }

            if (cellViewIndex < cellViewOffsetArray.Count)
            {
                if (insertPosition == CellViewPositionEnum.Before)
                {
                    return cellViewOffsetArray[cellViewIndex - 1] + spacing +
                           (scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left);
                }

                return cellViewOffsetArray[cellViewIndex] +
                       (scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left);
            }

            return cellViewOffsetArray[^2];
        }

        public float GetScrollPositionForDataIndex(int dataIndex, CellViewPositionEnum insertPosition)
        {
            return GetScrollPositionForCellViewIndex(loop ? mDelegate.GetNumberOfCells(this) + dataIndex : dataIndex,
                insertPosition);
        }

        public int GetCellViewIndexAtPosition(float position)
        {
            return _GetCellIndexAtPosition(position, 0, cellViewOffsetArray.Count - 1);
        }

        public EnhancedScrollerCellView GetCellViewAtDataIndex(int dataIndex)
        {
            foreach (var c in activeCellViews)
            {
                if (c.dataIndex == dataIndex)
                {
                    return c;
                }
            }

            return null;
        }

        public void ToggleTweenPaused(float newTweenTime = -1)
        {
            if (!tweenPaused)
            {
                tweenPaused = true;
                tweenPauseToggledOff = false;
            }
            else
            {
                tweenPaused = false;
                tweenPauseToggledOff = true;
                tweenPauseNewTweenTime = newTweenTime;
            }
        }

        public void InterruptTween()
        {
            if (IsTweening)
            {
                interruptTween = true;
            }
        }

        #endregion

        #region Private

        private bool initialized;

        private bool updateSpacing;

        private ScrollRect scrollRect;

        private RectTransform scrollRectTransform;

        private Scrollbar scrollbar;

        private RectTransform container;

        private HorizontalOrVerticalLayoutGroup layoutGroup;

        private IEnhancedScrollerDelegate mDelegate;

        private bool reloadData;

        private bool refreshActive;

        private readonly List<EnhancedScrollerCellView> recycledCellViews = new List<EnhancedScrollerCellView>();

        private LayoutElement firstPadding;

        private LayoutElement lastPadding;

        private RectTransform recycledCellViewContainer;

        private readonly List<float> cellViewSizeArray = new List<float>();

        private readonly List<float> cellViewOffsetArray = new List<float>();

        public float scrollPosition;

        private readonly List<EnhancedScrollerCellView> activeCellViews = new List<EnhancedScrollerCellView>();

        private int activeCellViewsStartIndex;

        private int activeCellViewsEndIndex;

        private int loopFirstCellIndex;

        private int loopLastCellIndex;

        private float loopFirstScrollPosition;

        private float loopLastScrollPosition;

        private float loopFirstJumpTrigger;

        private float loopLastJumpTrigger;

        private float lastScrollRectSize;

        private bool lastLoop;

        private ScrollbarVisibilityEnum lastScrollbarVisibility;
        private float singleLoopGroupSize;
        private bool loopBeforeDrag;
        private bool ignoreLoopJump;
        private int dragFingerCount;
        private bool interruptTween;
        private bool dragging;

        private enum ListPositionEnum
        {
            First,
            Last
        }

        private void Resize(bool keepPosition)
        {
            var originalScrollPosition = scrollPosition;

            cellViewSizeArray.Clear();
            var offset = _AddCellViewSizes();

            if (loop)
            {
                var cellCount = cellViewSizeArray.Count;

                if (offset < ScrollRectSize)
                {
                    var additionalRounds = Mathf.CeilToInt(Mathf.CeilToInt(ScrollRectSize / offset) / 2.0f) * 2;
                    _DuplicateCellViewSizes(additionalRounds, cellCount);
                    loopFirstCellIndex = cellCount * (1 + additionalRounds / 2);
                }
                else
                {
                    loopFirstCellIndex = cellCount;
                }

                loopLastCellIndex = loopFirstCellIndex + cellCount - 1;

                _DuplicateCellViewSizes(2, cellCount);
            }

            _CalculateCellViewOffsets();

            if (scrollDirection == ScrollDirectionEnum.Vertical)
                container.sizeDelta = new Vector2(container.sizeDelta.x,
                    cellViewOffsetArray[^1] + padding.top + padding.bottom);
            else
                container.sizeDelta = new Vector2(cellViewOffsetArray[^1] + padding.left + padding.right,
                    container.sizeDelta.y);

            if (loop)
            {
                loopFirstScrollPosition =
                    GetScrollPositionForCellViewIndex(loopFirstCellIndex, CellViewPositionEnum.Before) + spacing * 0.5f;
                loopLastScrollPosition =
                    GetScrollPositionForCellViewIndex(loopLastCellIndex, CellViewPositionEnum.After) - ScrollRectSize +
                    spacing * 0.5f;

                loopFirstJumpTrigger = loopFirstScrollPosition - ScrollRectSize;
                loopLastJumpTrigger = loopLastScrollPosition + ScrollRectSize;
            }

            ResetVisibleCellViews();

            if (keepPosition)
            {
                ScrollPosition = originalScrollPosition;
            }
            else
            {
                if (loop)
                {
                    ScrollPosition = loopFirstScrollPosition;
                }
                else
                {
                    ScrollPosition = 0;
                }
            }

            ScrollbarVisibility = scrollbarVisibility;
        }

        private void _UpdateSpacing(float spacing)
        {
            updateSpacing = false;
            layoutGroup.spacing = spacing;
            ReloadData(NormalizedScrollPosition);
        }

        private float _AddCellViewSizes()
        {
            var offset = 0f;
            singleLoopGroupSize = 0;
            for (var i = 0; i < NumberOfCells; i++)
            {
                cellViewSizeArray.Add(mDelegate.GetCellViewSize(this, i) + (i == 0 ? 0 : layoutGroup.spacing));
                singleLoopGroupSize += cellViewSizeArray[^1];
                offset += cellViewSizeArray[^1];
            }

            return offset;
        }

        private void _DuplicateCellViewSizes(int numberOfTimes, int cellCount)
        {
            for (var i = 0; i < numberOfTimes; i++)
            {
                for (var j = 0; j < cellCount; j++)
                {
                    cellViewSizeArray.Add(cellViewSizeArray[j] + (j == 0 ? layoutGroup.spacing : 0));
                }
            }
        }

        private void _CalculateCellViewOffsets()
        {
            cellViewOffsetArray.Clear();
            var offset = 0f;
            foreach (var size in cellViewSizeArray)
            {
                offset += size;
                cellViewOffsetArray.Add(offset);
            }
        }

        private EnhancedScrollerCellView GetRecycledCellView(EnhancedScrollerCellView cellPrefab)
        {
            for (var i = 0; i < recycledCellViews.Count; i++)
            {
                if (recycledCellViews[i].cellIdentifier == cellPrefab.cellIdentifier)
                {
                    var cellView = recycledCellViews[i];
                    recycledCellViews.RemoveAt(i);
                    return cellView;
                }
            }

            return null;
        }

        private void ResetVisibleCellViews()
        {
            CalculateCurrentActiveCellRange(out var startIndex, out var endIndex);

            var i = 0;
            var remainingCellIndices = new List<int>();
            while (i < activeCellViews.Count)
            {
                if (activeCellViews[i].cellIndex < startIndex || activeCellViews[i].cellIndex > endIndex)
                {
                    RecycleCell(activeCellViews[i]);
                }
                else
                {
                    remainingCellIndices.Add(activeCellViews[i].cellIndex);
                    i++;
                }
            }

            if (remainingCellIndices.Count == 0)
            {
                for (i = startIndex; i <= endIndex; i++)
                {
                    AddCellView(i, ListPositionEnum.Last);
                }
            }
            else
            {
                for (i = endIndex; i >= startIndex; i--)
                {
                    if (i < remainingCellIndices[0])
                    {
                        AddCellView(i, ListPositionEnum.First);
                    }
                }

                for (i = startIndex; i <= endIndex; i++)
                {
                    if (i > remainingCellIndices[^1])
                    {
                        AddCellView(i, ListPositionEnum.Last);
                    }
                }
            }

            activeCellViewsStartIndex = startIndex;
            activeCellViewsEndIndex = endIndex;

            SetLayouts();
        }

        private void RecycleAllCells()
        {
            while (activeCellViews.Count > 0) RecycleCell(activeCellViews[0]);
            activeCellViewsStartIndex = 0;
            activeCellViewsEndIndex = 0;
        }

        private void RecycleCell(EnhancedScrollerCellView cellView)
        {
            CellViewWillRecycle?.Invoke(cellView);

            activeCellViews.Remove(cellView);

            recycledCellViews.Add(cellView);

            cellView.transform.gameObject.SetActive(false);

            cellView.dataIndex = 0;
            cellView.cellIndex = 0;
            cellView.active = false;

            CellViewVisibilityChanged?.Invoke(cellView);
        }

        private void AddCellView(int cellIndex, ListPositionEnum listPosition)
        {
            if (NumberOfCells == 0) return;

            var dataIndex = cellIndex % NumberOfCells;
            var cellView = mDelegate.GetCellView(this, dataIndex, cellIndex);

            cellView.cellIndex = cellIndex;
            cellView.dataIndex = dataIndex;
            cellView.active = true;

            cellView.transform.SetParent(container, false);
            cellView.transform.localScale = Vector3.one;

            var layoutElement = cellView.GetComponent<LayoutElement>();
            if (!layoutElement) layoutElement = cellView.gameObject.AddComponent<LayoutElement>();

            if (scrollDirection == ScrollDirectionEnum.Vertical)
                layoutElement.minHeight = cellViewSizeArray[cellIndex] - (cellIndex > 0 ? layoutGroup.spacing : 0);
            else
                layoutElement.minWidth = cellViewSizeArray[cellIndex] - (cellIndex > 0 ? layoutGroup.spacing : 0);

            if (listPosition == ListPositionEnum.First)
                activeCellViews.Insert(0, cellView);
            else
                activeCellViews.Add(cellView);

            if (listPosition == ListPositionEnum.Last)
                cellView.transform.SetSiblingIndex(container.childCount - 2);
            else if (listPosition == ListPositionEnum.First)
                cellView.transform.SetSiblingIndex(1);

            CellViewVisibilityChanged?.Invoke(cellView);
        }

        private void SetLayouts()
        {
            if (NumberOfCells == 0) return;

            var firstSize = cellViewOffsetArray[activeCellViewsStartIndex] -
                            cellViewSizeArray[activeCellViewsStartIndex];
            var lastSize = cellViewOffsetArray[^1] - cellViewOffsetArray[activeCellViewsEndIndex];

            if (scrollDirection == ScrollDirectionEnum.Vertical)
            {
                firstPadding.minHeight = firstSize;
                firstPadding.gameObject.SetActive(firstPadding.minHeight > 0);

                lastPadding.minHeight = lastSize;
                lastPadding.gameObject.SetActive(lastPadding.minHeight > 0);
            }
            else
            {
                firstPadding.minWidth = firstSize;
                firstPadding.gameObject.SetActive(firstPadding.minWidth > 0);

                lastPadding.minWidth = lastSize;
                lastPadding.gameObject.SetActive(lastPadding.minWidth > 0);
            }
        }

        private void _RefreshActive()
        {
            if (loop && !ignoreLoopJump)
            {
                Vector2 velocity;
                if (scrollPosition < loopFirstJumpTrigger)
                {
                    velocity = scrollRect.velocity;
                    ScrollPosition = loopLastScrollPosition - (loopFirstJumpTrigger - scrollPosition) + spacing;
                    scrollRect.velocity = velocity;
                }
                else if (scrollPosition > loopLastJumpTrigger)
                {
                    velocity = scrollRect.velocity;
                    ScrollPosition = loopFirstScrollPosition + (scrollPosition - loopLastJumpTrigger) - spacing;
                    scrollRect.velocity = velocity;
                }
            }

            CalculateCurrentActiveCellRange(out var startIndex, out var endIndex);

            if (startIndex == activeCellViewsStartIndex && endIndex == activeCellViewsEndIndex) return;

            ResetVisibleCellViews();
        }

        private void CalculateCurrentActiveCellRange(out int startIndex, out int endIndex)
        {
            startIndex = 0;
            endIndex = 0;

            var startPosition = scrollPosition - LookAheadBefore + CalculateStartCellBias;
            var endPosition = scrollPosition +
                              (scrollDirection == ScrollDirectionEnum.Vertical
                                  ? scrollRectTransform.rect.height
                                  : scrollRectTransform.rect.width) + LookAheadAfter;

            startIndex = GetCellViewIndexAtPosition(startPosition);
            endIndex = GetCellViewIndexAtPosition(endPosition);
        }

        private int _GetCellIndexAtPosition(float position, int startIndex, int endIndex)
        {
            while (true)
            {
                if (startIndex >= endIndex) return startIndex;

                var middleIndex = (startIndex + endIndex) / 2;

                var pad = scrollDirection == ScrollDirectionEnum.Vertical ? padding.top : padding.left;
                if (cellViewOffsetArray[middleIndex] + pad >= position + (pad == 0 ? 0 : 1.00001f))
                {
                    endIndex = middleIndex;
                    continue;
                }

                startIndex = middleIndex + 1;
            }
        }

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            scrollRectTransform = scrollRect.GetComponent<RectTransform>();

            if (scrollRect.content != null)
            {
                DestroyImmediate(scrollRect.content.gameObject);
            }

            var go = new GameObject("Container", typeof(RectTransform));
            go.transform.SetParent(scrollRectTransform);
            if (scrollDirection == ScrollDirectionEnum.Vertical)
                go.AddComponent<VerticalLayoutGroup>();
            else
                go.AddComponent<HorizontalLayoutGroup>();
            container = go.GetComponent<RectTransform>();

            if (scrollDirection == ScrollDirectionEnum.Vertical)
            {
                container.anchorMin = new Vector2(0, 1);
                container.anchorMax = Vector2.one;
                container.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = new Vector2(0, 1f);
                container.pivot = new Vector2(0, 0.5f);
            }

            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
            container.offsetMax = Vector2.zero;
            container.offsetMin = Vector2.zero;

            scrollRect.content = container;

            scrollbar = scrollDirection == ScrollDirectionEnum.Vertical
                ? scrollRect.verticalScrollbar
                : scrollRect.horizontalScrollbar;

            layoutGroup = container.GetComponent<HorizontalOrVerticalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.childForceExpandWidth = true;

            scrollRect.horizontal = scrollDirection == ScrollDirectionEnum.Horizontal;
            scrollRect.vertical = scrollDirection == ScrollDirectionEnum.Vertical;


            go = new GameObject("First Padding", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(container, false);
            firstPadding = go.GetComponent<LayoutElement>();

            go = new GameObject("Last Padding", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(container, false);
            lastPadding = go.GetComponent<LayoutElement>();

            go = new GameObject("Recycled Cells", typeof(RectTransform));
            go.transform.SetParent(scrollRect.transform, false);
            recycledCellViewContainer = go.GetComponent<RectTransform>();
            recycledCellViewContainer.gameObject.SetActive(false);

            lastScrollRectSize = ScrollRectSize;
            lastLoop = loop;
            lastScrollbarVisibility = scrollbarVisibility;

            initialized = true;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (IsTweening && interruptTweeningOnPointerDown)
            {
                interruptTween = true;
            }
        }

        public void OnBeginDrag(PointerEventData data)
        {
            dragging = true;

            dragFingerCount++;
            if (dragFingerCount > 1) return;

            loopBeforeDrag = loop;
            if (!loopWhileDragging)
            {
                loop = false;
            }

            if (IsTweening && interruptTweeningOnDrag)
            {
                interruptTween = true;
            }
        }

        public void OnEndDrag(PointerEventData data)
        {
            dragging = false;

            dragFingerCount--;
            if (dragFingerCount < 0) dragFingerCount = 0;

            loop = loopBeforeDrag;
        }

        private void Update()
        {
            if (updateSpacing)
            {
                _UpdateSpacing(spacing);
                reloadData = false;
            }

            if (reloadData)
            {
                ReloadData();
            }

            if ((loop && !Mathf.Approximately(lastScrollRectSize, ScrollRectSize)) || loop != lastLoop)
            {
                Resize(true);
                lastScrollRectSize = ScrollRectSize;

                lastLoop = loop;
            }

            if (lastScrollbarVisibility != scrollbarVisibility)
            {
                ScrollbarVisibility = scrollbarVisibility;
                lastScrollbarVisibility = scrollbarVisibility;
            }

            if (LinearVelocity != 0 && !IsScrolling)
            {
                IsScrolling = true;
                ScrollerScrollingChanged?.Invoke(this, true);
            }
            else if (LinearVelocity == 0 && IsScrolling)
            {
                IsScrolling = false;
                ScrollerScrollingChanged?.Invoke(this, false);
            }
        }

        private void OnValidate()
        {
            if (initialized && !Mathf.Approximately(spacing, layoutGroup.spacing))
            {
                updateSpacing = true;
            }
        }

        private void LateUpdate()
        {
            if (maxVelocity > 0)
            {
                if (scrollDirection == ScrollDirectionEnum.Horizontal)
                {
                    Velocity = new Vector2(Mathf.Clamp(Mathf.Abs(Velocity.x), 0, maxVelocity) * Mathf.Sign(Velocity.x),
                        Velocity.y);
                }
                else
                {
                    Velocity = new Vector2(Velocity.x,
                        Mathf.Clamp(Mathf.Abs(Velocity.y), 0, maxVelocity) * Mathf.Sign(Velocity.y));
                }
            }
        }

        private void OnEnable()
        {
            scrollRect.onValueChanged.AddListener(_ScrollRect_OnValueChanged);
        }

        private void OnDisable()
        {
            scrollRect.onValueChanged.RemoveListener(_ScrollRect_OnValueChanged);
        }

        private void _ScrollRect_OnValueChanged(Vector2 val)
        {
            if (scrollDirection == ScrollDirectionEnum.Vertical)
                scrollPosition = (1f - val.y) * ScrollSize;
            else
                scrollPosition = val.x * ScrollSize;
            scrollPosition = Mathf.Clamp(scrollPosition, 0, ScrollSize);

            ScrollerScrolled?.Invoke(this, val, scrollPosition);

            _RefreshActive();
        }

        #endregion

        #region Tweening

        public enum TweenType
        {
            immediate,
            linear,
            spring,
            easeInQuad,
            easeOutQuad,
            easeInOutQuad,
            easeInCubic,
            easeOutCubic,
            easeInOutCubic,
            easeInQuart,
            easeOutQuart,
            easeInOutQuart,
            easeInQuint,
            easeOutQuint,
            easeInOutQuint,
            easeInSine,
            easeOutSine,
            easeInOutSine,
            easeInExpo,
            easeOutExpo,
            easeInOutExpo,
            easeInCirc,
            easeOutCirc,
            easeInOutCirc,
            easeInBounce,
            easeOutBounce,
            easeInOutBounce,
            easeInBack,
            easeOutBack,
            easeInOutBack,
            easeInElastic,
            easeOutElastic,
            easeInOutElastic,
            custom
        }

        private float tweenTimeLeft;
        private bool tweenPauseToggledOff;
        private float tweenPauseNewTweenTime;

        private IEnumerator TweenPosition(TweenType tweenType, float time, float start, float end, Action tweenComplete,
            bool forceCalculateRange)
        {
            if (!(tweenType == TweenType.immediate || time == 0))
            {
                scrollRect.velocity = Vector2.zero;

                IsTweening = true;
                ScrollerTweeningChanged?.Invoke(this, true);

                tweenTimeLeft = 0;
                var newPosition = 0f;

                while (tweenTimeLeft < time && !interruptTween)
                {
                    if (!tweenPaused)
                    {
                        if (tweenPauseToggledOff)
                        {
                            tweenPauseToggledOff = false;
                            start = ScrollPosition;
                            time = tweenPauseNewTweenTime < 0 ? tweenTimeLeft : tweenPauseNewTweenTime;
                            tweenTimeLeft = 0;
                        }

                        switch (tweenType)
                        {
                            case TweenType.linear: newPosition = Linear(start, end, tweenTimeLeft / time); break;
                            case TweenType.spring: newPosition = Spring(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInQuad:
                                newPosition = EaseInQuad(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutQuad:
                                newPosition = EaseOutQuad(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutQuad:
                                newPosition = EaseInOutQuad(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInCubic:
                                newPosition = EaseInCubic(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutCubic:
                                newPosition = EaseOutCubic(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutCubic:
                                newPosition = EaseInOutCubic(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInQuart:
                                newPosition = EaseInQuart(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutQuart:
                                newPosition = EaseOutQuart(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutQuart:
                                newPosition = EaseInOutQuart(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInQuint:
                                newPosition = EaseInQuint(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutQuint:
                                newPosition = EaseOutQuint(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutQuint:
                                newPosition = EaseInOutQuint(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInSine:
                                newPosition = EaseInSine(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutSine:
                                newPosition = EaseOutSine(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutSine:
                                newPosition = EaseInOutSine(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInExpo:
                                newPosition = EaseInExpo(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutExpo:
                                newPosition = EaseOutExpo(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutExpo:
                                newPosition = EaseInOutExpo(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInCirc:
                                newPosition = EaseInCirc(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutCirc:
                                newPosition = EaseOutCirc(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutCirc:
                                newPosition = EaseInOutCirc(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInBounce:
                                newPosition = EaseInBounce(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutBounce:
                                newPosition = EaseOutBounce(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutBounce:
                                newPosition = EaseInOutBounce(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInBack:
                                newPosition = EaseInBack(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutBack:
                                newPosition = EaseOutBack(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutBack:
                                newPosition = EaseInOutBack(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInElastic:
                                newPosition = EaseInElastic(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeOutElastic:
                                newPosition = EaseOutElastic(start, end, tweenTimeLeft / time); break;
                            case TweenType.easeInOutElastic:
                                newPosition = EaseInOutElastic(start, end, tweenTimeLeft / time); break;
                            case TweenType.custom:
                                newPosition = CustomTweenFunction?.Invoke(start, end, tweenTimeLeft / time) ??
                                              Linear(start, end, tweenTimeLeft / time); break;
                        }

                        ScrollPosition = newPosition;

                        tweenTimeLeft += Time.unscaledDeltaTime;
                    }

                    yield return null;
                }
            }

            if (interruptTween)
            {
                interruptTween = false;

                IsTweening = false;
                ScrollerTweeningChanged?.Invoke(this, false);
            }
            else
            {
                ScrollPosition = end;

                if (forceCalculateRange || tweenType == TweenType.immediate || time == 0)
                {
                    _RefreshActive();
                }

                tweenComplete?.Invoke();

                IsTweening = false;
                ScrollerTweeningChanged?.Invoke(this, false);
            }
        }


        private static float Linear(float start, float end, float val)
        {
            return Mathf.Lerp(start, end, val);
        }

        private static float Spring(float start, float end, float val)
        {
            val = Mathf.Clamp01(val);
            val = (Mathf.Sin(val * Mathf.PI * (0.2f + 2.5f * val * val * val)) * Mathf.Pow(1f - val, 2.2f) + val) *
                  (1f + 1.2f * (1f - val));
            return start + (end - start) * val;
        }

        private static float EaseInQuad(float start, float end, float val)
        {
            end -= start;
            return end * val * val + start;
        }

        private static float EaseOutQuad(float start, float end, float val)
        {
            end -= start;
            return -end * val * (val - 2) + start;
        }

        private static float EaseInOutQuad(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return end / 2 * val * val + start;
            val--;
            return -end / 2 * (val * (val - 2) - 1) + start;
        }

        private static float EaseInCubic(float start, float end, float val)
        {
            end -= start;
            return end * val * val * val + start;
        }

        private static float EaseOutCubic(float start, float end, float val)
        {
            val--;
            end -= start;
            return end * (val * val * val + 1) + start;
        }

        private static float EaseInOutCubic(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return end / 2 * val * val * val + start;
            val -= 2;
            return end / 2 * (val * val * val + 2) + start;
        }

        private static float EaseInQuart(float start, float end, float val)
        {
            end -= start;
            return end * val * val * val * val + start;
        }

        private static float EaseOutQuart(float start, float end, float val)
        {
            val--;
            end -= start;
            return -end * (val * val * val * val - 1) + start;
        }

        private static float EaseInOutQuart(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return end / 2 * val * val * val * val + start;
            val -= 2;
            return -end / 2 * (val * val * val * val - 2) + start;
        }

        private static float EaseInQuint(float start, float end, float val)
        {
            end -= start;
            return end * val * val * val * val * val + start;
        }

        private static float EaseOutQuint(float start, float end, float val)
        {
            val--;
            end -= start;
            return end * (val * val * val * val * val + 1) + start;
        }

        private static float EaseInOutQuint(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return end / 2 * val * val * val * val * val + start;
            val -= 2;
            return end / 2 * (val * val * val * val * val + 2) + start;
        }

        private static float EaseInSine(float start, float end, float val)
        {
            end -= start;
            return -end * Mathf.Cos(val / 1 * (Mathf.PI / 2)) + end + start;
        }

        private static float EaseOutSine(float start, float end, float val)
        {
            end -= start;
            return end * Mathf.Sin(val / 1 * (Mathf.PI / 2)) + start;
        }

        private static float EaseInOutSine(float start, float end, float val)
        {
            end -= start;
            return -end / 2 * (Mathf.Cos(Mathf.PI * val / 1) - 1) + start;
        }

        private static float EaseInExpo(float start, float end, float val)
        {
            end -= start;
            return end * Mathf.Pow(2, 10 * (val / 1 - 1)) + start;
        }

        private static float EaseOutExpo(float start, float end, float val)
        {
            end -= start;
            return end * (-Mathf.Pow(2, -10 * val / 1) + 1) + start;
        }

        private static float EaseInOutExpo(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return end / 2 * Mathf.Pow(2, 10 * (val - 1)) + start;
            val--;
            return end / 2 * (-Mathf.Pow(2, -10 * val) + 2) + start;
        }

        private static float EaseInCirc(float start, float end, float val)
        {
            end -= start;
            return -end * (Mathf.Sqrt(1 - val * val) - 1) + start;
        }

        private static float EaseOutCirc(float start, float end, float val)
        {
            val--;
            end -= start;
            return end * Mathf.Sqrt(1 - val * val) + start;
        }

        private static float EaseInOutCirc(float start, float end, float val)
        {
            val /= .5f;
            end -= start;
            if (val < 1) return -end / 2 * (Mathf.Sqrt(1 - val * val) - 1) + start;
            val -= 2;
            return end / 2 * (Mathf.Sqrt(1 - val * val) + 1) + start;
        }

        private static float EaseInBounce(float start, float end, float val)
        {
            end -= start;
            var d = 1f;
            return end - EaseOutBounce(0, end, d - val) + start;
        }

        private static float EaseOutBounce(float start, float end, float val)
        {
            val /= 1f;
            end -= start;
            if (val < 1 / 2.75f)
            {
                return end * (7.5625f * val * val) + start;
            }

            if (val < 2 / 2.75f)
            {
                val -= 1.5f / 2.75f;
                return end * (7.5625f * val * val + .75f) + start;
            }

            if (val < 2.5 / 2.75)
            {
                val -= 2.25f / 2.75f;
                return end * (7.5625f * val * val + .9375f) + start;
            }

            val -= 2.625f / 2.75f;
            return end * (7.5625f * val * val + .984375f) + start;
        }

        private static float EaseInOutBounce(float start, float end, float val)
        {
            end -= start;
            var d = 1f;
            if (val < d / 2) return EaseInBounce(0, end, val * 2) * 0.5f + start;
            return EaseOutBounce(0, end, val * 2 - d) * 0.5f + end * 0.5f + start;
        }

        private static float EaseInBack(float start, float end, float val)
        {
            end -= start;
            val /= 1;
            var s = 1.70158f;
            return end * val * val * ((s + 1) * val - s) + start;
        }

        private static float EaseOutBack(float start, float end, float val)
        {
            var s = 1.70158f;
            end -= start;
            val = val / 1 - 1;
            return end * (val * val * ((s + 1) * val + s) + 1) + start;
        }

        private static float EaseInOutBack(float start, float end, float val)
        {
            var s = 1.70158f;
            end -= start;
            val /= .5f;
            if (val < 1)
            {
                s *= 1.525f;
                return end / 2 * (val * val * ((s + 1) * val - s)) + start;
            }

            val -= 2;
            s *= 1.525f;
            return end / 2 * (val * val * ((s + 1) * val + s) + 2) + start;
        }

        private static float EaseInElastic(float start, float end, float val)
        {
            end -= start;

            var d = 1f;
            var p = d * .3f;
            float s;
            float a = 0;

            if (val == 0) return start;
            val /= d;
            if (Mathf.Approximately(val, 1)) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            val -= 1;
            return -(a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
        }

        private static float EaseOutElastic(float start, float end, float val)
        {
            end -= start;

            var d = 1f;
            var p = d * .3f;
            float s;
            float a = 0;

            if (val == 0) return start;

            val /= d;
            if (Mathf.Approximately(val, 1)) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            return a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) + end + start;
        }

        private static float EaseInOutElastic(float start, float end, float val)
        {
            end -= start;

            var d = 1f;
            var p = d * .3f;
            float s;
            float a = 0;

            if (val == 0) return start;

            val /= d / 2;
            if (Mathf.Approximately(val, 2)) return start + end;

            if (a == 0f || a < Mathf.Abs(end))
            {
                a = end;
                s = p / 4;
            }
            else
            {
                s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
            }

            if (val < 1)
            {
                val -= 1;
                return -0.5f * (a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
            }

            val -= 1;
            return a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
        }

        #endregion
    }
}