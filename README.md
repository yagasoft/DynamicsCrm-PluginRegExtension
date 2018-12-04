# DynamicsCrm-PluginRegExtension

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension](https://badges.gitter.im/yagasoft/DynamicsCrm-PluginRegExtension.svg)](https://gitter.im/yagasoft/DynamicsCrm-PluginRegExtension?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 1.13.5
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

+ Right-click plugin project and choose 'register' or 'update'

## Changes

#### _v1.13.5 (2018-12-04)_
+ Fixed: thread deadlock
#### _v1.13.4 (2018-09-13)_
+ Fixed: show missing messages in some entities
+ Fixed: image attribute list empty on first access to dialogue
+ Fixed: updating image throws exception

---
**Copyright &copy; by Ahmed el-Sawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_
