# DynamicsCrm-PluginRegExtension

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension](https://badges.gitter.im/yagasoft/DynamicsCrm-PluginRegExtension.svg)](https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

---

A Visual Studio extension that reduces the hassle of using the official plugin tool, so it is much easier and faster to update plugins.

## Features

  + Register a plugin/step from inside Visual Studio
  + Automatically builds the project before registering/updating the assembly
  + Update multiple plugin/step assemblies at once
  + Auto-name steps to be meaningful
  + Filter messages to what the entity supports
  + Prevent registering steps/images for unsupported configurations

## Guide

+ Install the Visual Studio extension ([here](https://marketplace.visualstudio.com/items?itemName=Yagasoft.CrmPluginRegExt))
+ Right-click a plugin project and choose 'register' or 'update'

## Changes
+ Check Releases page for the later changes
#### _v1.13.4 to v2.4.1 (since 2018-09-13)_
+ Added: filtering
+ Added: list of non-existent plugin types to the popup message
+ Changed: switched to explicit Connection Strings to allow for a broader support of newer features
+ Changed: save settings as JSON (at the solution level)
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: SDK packages
+ Updated: Controls library
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: licence
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: Common assembly reference
+ Improved: switched to EnhancedOrgService package for improved connection pooling and caching
+ Improved: performance
+ Improved: UI layout
+ Improved: connection pooling
+ Improved: connection caching
+ Improved: separated the connection save file to facilitate excluding from source control
+ Improved: stability of initiating connections
+ Improved: speed of refreshing types
+ Improved: connection readiness performance
+ Improved: upgraded to VS Async API
+ Fixed: show missing messages in some entities
+ Fixed: image attribute list empty on first access to dialogue
+ Fixed: updating image throws exception
+ Fixed: thread deadlock
+ Fixed: issue with assembly binding
+ Fixed: connection string values containing '=' character causing connectivity issue (e.g. client secrets containing '=')
+ Fixed: connection errors causing deadlocks
+ Fixed: settings not saved properly
+ Fixed: duplicating settings causes issue with saved data
+ Fixed: connection issues
+ Fixed: clear cache
+ Fixed: non-existent entities in settings persisting, causing error with 'generate cached'
+ Fixed: random range error
+ Removed: 'refresh' of WF steps, as it doesn't work in v9.1 (increment the assembly version instead)
#### _v1.1.1 (2015-06-05)_

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_
