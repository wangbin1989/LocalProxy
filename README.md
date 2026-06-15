# Local Proxy

一款本地代理工具，用于将本地端口代理到服务器，支持 TCP/UDP/HTTP 代理。提供 CLI 和 GUI 两种管理方式，适用于 macOS、Windows 和 Linux 平台。

## 技术栈

- 核心框架：.NET 10 AOT
- 组件：MAUI, System.CommandLine, Spectre.Console

## 运行环境

- macOS (Apple Silicon)
- Windows (x64, arm64)
- Linux (x64, arm64), 仅 CLI 版本

## 功能特性

- 支持 TCP/UDP/HTTP 代理
- 支持多平台（macOS、Windows、Linux）
- 提供 CLI 和 GUI 两种管理方式
- 预置常用软件代理配置，如 SQL Server、Redis 等
- 支持自定义代理配置
- 软件以服务方式运行，后台自动重启
- GUI 界面支持最小化到系统托盘，支持开机自启
