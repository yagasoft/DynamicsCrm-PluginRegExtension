# DynamicsCrm-PluginRegExtension

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension](https://badges.gitter.im/yagasoft/DynamicsCrm-PluginRegExtension.svg)](https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 2.3.2
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

#### _v2.3.2 (2021-10-11)_
+ Updated: Common assembly reference
#### _v2.3.1 (2021-10-04)_
+ Improved: upgraded to VS Async API
+ Fixed: non-existent entities in settings persisting, causing error with 'generate cached'
#### _v2.2.4 (2021-04-29)_
+ Improved: connection readiness performance
#### _v2.2.3 (2020-12-08)_
+ Fixed: clear cache
#### _v2.2.2 (2020-11-22)_
+ Improved: stability of initiating connections
+ Improved: speed of refreshing types
+ Updated: SDK to match other tools to avoid conflicts
#### _v2.2.1 (2020-11-11)_
+ Added: list of non-existent plugin types to the popup message
#### _v2.1.9 (2020-10-14)_
+ Fixed: connection issues
#### _v2.1.8 (2020-10-11)_
+ Fixed: duplicating settings causes issue with saved data
#### _v2.1.7 (2020-10-04)_
+ Fixed: connection errors causing deadlocks
+ Fixed: settings not saved properly
#### _v2.1.6 (2020-10-01)_
+ Improved: separated the connection save file to facilitate excluding from source control
+ Fixed: connection string values containing '=' character causing connectivity issue (e.g. client secrets containing '=')
#### _v2.1.5 (2020-09-15)_
+ Improved: connection caching
#### _v2.1.4 (2020-08-28)_
+ Updated: SDK to match other tools to avoid conflicts
#### _v2.1.3 (2020-08-25)_
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: licence
+ Fixed: issue with assembly binding
#### _v2.1.2 (2020-08-13)_
+ Updated: SDK to match other tools to avoid conflicts
+ Updated: Controls library
+ Improved: connection pooling
+ Fixed: issues
#### _v2.1.1 (2020-08-10)_
+ Added: filtering
+ Changed: switched to explicit Connection Strings to allow for a broader support of newer features
+ Changed: save settings as JSON (at the solution level)
+ Updated: SDK packages
+ Improved: switched to EnhancedOrgService package for improved connection pooling and caching
+ Improved: performance
+ Improved: UI layout
+ Fixed: issues
#### _v1.13.6 (2018-12-18)_
+ Fixed: thread deadlock
#### _v1.13.4 (2018-09-13)_
+ Fixed: show missing messages in some entities
+ Fixed: image attribute list empty on first access to dialogue
+ Fixed: updating image throws exception

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_
