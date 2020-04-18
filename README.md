# Shattered Kingdoms

This mod makes (almost) every clan in every faction become their own kingdom and go to war with each other.

Help is always welcome, check out the known issues and make a pull request or report a new issue. 


## Update

This past week I've been working hard at trying to make kingdoms dynamically shatter and I'm pretty close.

What I have so far is that if a kingdom grows to big, a pretty big army from a randomly generated clan and kingdom spawns near a city and besieges it.

If it wins, it gets the city and if it doesn't, it atleast weakend the bloated kingdom.

There are issues however, some critical, some less.

The biggest issue is that the game randomly crashes after a few weeks, or a few days, it varies. What causes this is unknown to me, because it's not an exception from within my code (but it is definitely because of it).

If someone could let me know how to or where to find information on how to debug crash reasons (The TW generated crash logs are useless to me)

Developing for Bannerlord is currently very finicky. For example, I wanted the army that spawns not to starve, so I added food similar to how TaleWorlds adds food to bandit parties, but that causes the game to become unsavable. That was fun to debug.

I need help from you guys to figure this out, all the relevant code is available on the dev branch from my github right here. The dynamic shatter is in the ShatterBehavior.cs file.

I would be incredibly grateful if anyone could give it a look.


### Installation

Drop the ShatteredKingdoms folder containing the SubModule.xml, bin and ModuleData in your M&B Bannerlord Modules folder, enable it in the launcher and create a new campaign.

Also check if the .dll in the bin folder is not blocked by windows by right clicking on it and checking properties.


### Changelog

1.0.1
* Changed colors for kingdoms and clans, feedback/suggestions are welcome


### Known Issues

1. The original kingdom leader cannot unjoin his own faction
2. Clans which only own a castle cannot become their own kingdom. I haven't figured out how to give them a town, help appreciated. This results in the original kingdom having more clans than the ones who break off.


### Reported Issues

1. A report of high RAM to crash at campaign start
2. Possible mod conflict with DiplomacyReworked https://www.nexusmods.com/mountandblade2bannerlord/mods/427


### Thanks

Thanks to calsev for his template mod that got me started. https://github.com/calsev/bannerlord_smith_forever. 
