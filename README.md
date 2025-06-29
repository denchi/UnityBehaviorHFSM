# Behaviour HFSM Module for Unity

This project is a Unity-based package containing various modules for behavior management, including dialog, graph, HFSM, tree, and variable systems. It is currently under active development and is a work in progress.

## HFSM Module Details

The `com.deathbygravitystudio.behaviour.hfsm` package provides a modular Hierarchical Finite State Machine (HFSM) system for Unity. It is designed for advanced state management, supporting nested states, transitions, and extensibility via services and custom state types. Key features include:

- **Node-based Architecture:** Each state is represented as a node (ScriptableObject) that can contain transitions, services, and hierarchical relationships (parent, layer).
- **Extensible State Interfaces:** Includes interfaces for different state types, such as `IBaseState`, `IAnimatableState`, `IComposedState`, and `ITimedState`, allowing for flexible and composable state logic.
- **Services and Utilities:** Supports attaching services (via `IService`) and timeouts to states for advanced behaviors.
- **Sample Scene:** Comes with a sample scene demonstrating practical usage of the HFSM system.
- **Shared Variable System:** Integrates with the shared variable package for state data management.

This module is suitable for implementing complex AI, gameplay, or workflow systems in Unity projects.

## Note
This project is a work in progress. Features and documentation are subject to change.
