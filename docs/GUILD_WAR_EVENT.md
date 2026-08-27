# Guild War & Battle Event Specification

## Overview
The Guild War Event system allows Game Masters and Guild Leaders to trigger server-wide guild battles and controlled PvP events in Season 6.

## Chat Commands
- `/warevent`: GM command to toggle server-wide Guild War event state on/off.
- `/war <GuildName>`: Initiates a standard guild war challenge against target guild.
- `/battlesoccer <GuildName>`: Initiates a Battle Soccer match in Arena (Stadium).

## Architecture & Integration
- **GuildWarEventChatCommandPlugIn**: Located in `MUnique.OpenMU.GameLogic.PlugIns.ChatCommands`.
- **BotPvpRules**: Automated bot players follow strict safety rules (`IsLegalPvpTarget`) to ensure legal targeting without unintended PK escalation.
- **Offline Combat Handlers**: Bots and offline helpers respect event bounds and safe zones during active war events.
