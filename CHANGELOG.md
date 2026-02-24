# Changelog
## [1.5.0] - 2025-11-28
- simplify async init
## [1.4.7] - 2025-11-27
- Introduces BindableCollection, BindableList, and BindableDictionary classes to support data binding for collections, lists, and dictionaries. Refactors and extends extension methods for collection operations to work with the new bindable types, enabling notification on collection changes.
## [1.4.6] - 2025-11-26
- Add BindableProperty Extension for ICollection<T>, IList<T>, HashSet<T>
## [1.4.5] - 2025-09-29
- Add ForceNotify and UnRegisterAll to BindableProperty
## [1.4.4] - 2025-08-28
- Refactor UpdateUI method in BindablePropertyButton
## [1.4.3] - 2025-06-13
- Rename SendCommandOnlyArgs method to SendCommand for consistency
## [1.4.2] - 2025-06-12
- refactor ICommand interfaces for improved execution handling
## [1.3.4] - 2025-06-03
- refactor: improve service initialization logic in ServiceLocator and AsyncSupport
## [1.3.1] - 2025-05-27
- refactor event handling in ServiceLocator to use Dictionary<Type, Delegate>
## [1.3.0] - 2025-05-27
- enhance ICommand interface to support variable arguments and update ServiceLocator to manage command instances
## [1.2.2] - 2025-05-27
- refactor event handling to use delegates for improved performance and flexibility
## [1.2.1] - 2025-04-18
- add: `Cache`
## [1.1.1] - 2025-04-16
- remove: `EnhancedScroller` from the package
## [1.1.0] - 2025-04-16
- add: `EnhancedScroller`
## [1.0.3] - 2025-04-15
- add: `lastSelected` field to `BindablePropertyButton`
## [1.0.1] - 2025-03-19
### Add BindablePropertyButton
## [1.0.0] - 2025-03-07
### This is the first release of *com.rickit.rframework*.
- first commit
