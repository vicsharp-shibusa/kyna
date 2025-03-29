# Kyna

[Kyna](https://www.theparentz.com/baby-names/kyna) is an MIT-licensed, open-source project with the goal of creating a reliable framework from which coders/traders/investors can collect historical stock data, backtest trading ideas against that data, and make informed decisions with their money.

What we're doing here is not groundbreaking science, but it is 100% transparent and grounded in a set of tools available to retail investors.
Everyone is welcome to a copy of the code repository for their own purposes.
Those who want to *contribute* are invited to do so - we welcome the help from any interested and capable party.

If the development process or the thought processes behind the decisions are of interest to you, such things are being actively chronicled on the [Vic Sharp YouTube Channel - Kyna Project Playlist](https://www.youtube.com/playlist?list=PLGw44r0iH8bayhAUZsMaK15Ny7--x8Mq_). *Don't forget to like and subscribe.*

## Designs

Kyna is intended to be a suite of software tools that

1. collect historical pricing and volume data for equity markets,
2. collect financial news,
3. analyze the efficacy of tradeable signals,
4. report on results of backtesting analysis,
5. and identify possible points of timely interest within the market.

### Context

![Kyna C4 Context](./docs/images/kyna-context.png)

### Architecture

![Kyna Architecture](./docs/images/kyna-architecture.png)

---

## Project Management

### Current Project Phase

It's been a minute since I worked on this project, but I think I'm ready for another round with it.
I've been thinking about it.

I spent some time last year toying with the idea of a "game version" in which "players" could be constructed with different goals and strategies.
The "game board" would be a set of stocks over a period of time and the winner would be the player with the most money at the end.
Players would, of course, have equal account values at the start (i.e., the same amount of "money").
I still think this is a good idea, but it's a massive undertaking.
Buying strategies are not too difficult, but selling strategies are another level.
And trying to manage a portfolio (e.g., making decisions about when to sell X in order to afford buying Y) is another level beyond.

So, back to earth.
The next test is going to be built around "swing trading" with a 3-week prologue and epilogue.
See [the project on GitHub](https://github.com/users/vicsharp-shibusa/projects/5).
The plan is to work out the test using stocks, develop some very specific sell rules, and then see if we can't also work in option data analysis as a _stretch goal_.
I want to accomplish this without disturbing the existing code, meaning preserving backward compatibility.
At least for now - if the model that is the output from this phase accomplishes its goals, we could sunset the existing model without losing the ability to re-create its outputs.

The big thrust of the current phase is _more signals_.
I'll resurrect the `ChartReader` concept where each `ChartReader` instance reads a `Chart` for a specific thing and offers up `IEnumerable<Signal>` or some such thing.
It's a good chance to work on _parallelization_.

There's probably a lot more Python in this phase - to take advantage of its ability to generate charts.
I believe we'll want to see charts overlayed with the _signals_ collected.
I'm going to use the existing dotnet framework (and the existing database) I've built to collect and organize the data for easy access from Python.

### Project History

The project started in the second half of January, 2024.
We began [Phase 0](https://github.com/vicsharp-shibusa/kyna/milestone/1) by creating the [public repo](https://github.com/vicsharp-shibusa/kyna) and shelling out the initial architecture for the code base.
A simple [design doc](https://github.com/vicsharp-shibusa/kyna/blob/main/docs/designs.md) was composed.
We constructed a system for logging and a database-agnostic context solution, and worked out the standards for PostgreSQL database access.
The `Kyna.Prototype.Cli` was constructed; its purpose was to be the place in which solutions and short-term needs could be managed in a "throw-away" context.
The [Phase 0](https://github.com/vicsharp-shibusa/kyna/milestone/1) milestone closed on February 2, 2024.

[Phase 1.0](https://github.com/vicsharp-shibusa/kyna/milestone/2) consisted of constructing the first draft of the `EodHdImporter`, a utility for fetching market data from [eodhd.com](https://eodhd.com/).
Using [eodhd.com](https://eodhd.com/) as our model, we built the `imports` database and the `api_transactions` table therein.
Data is captured from the [eodhd.com](https://eodhd.com/) API and HTTP JSON reponses are stored in the `api_transactions` table.
We could have chosen to migrate this data directly into its final form, but we chose this intermediate step because the [eodhd.com](https://eodhd.com/) API is a credit-based system (each call requires some number of credits) with a daily limit of 100,000.
We did not want failures in the migration (to the final form of the data) to result in having to re-fetch and re-spend the credits.
Therefore, we employed the principle of *separation of concerns* and isolated the first task to simply capturing the required data and preserving the resulting JSON - migration could be handled in a second step.
[Phase 1.0](https://github.com/vicsharp-shibusa/kyna/milestone/2) was closed on February 4, 2024.

[Phase 1.1](https://github.com/vicsharp-shibusa/kyna/milestone/3) consisted of constructing the database and models for the final state of the data collected from [eodhd.com](https://eodhd.com/) and building `Kyna.Migrator.Cli` to facilitate the migration from the `imports.api_transactions` table to the relevant tables in the `financials` database.
[Phase 1.1](https://github.com/vicsharp-shibusa/kyna/milestone/3) was closed on February 18, 2024.

[Phase 1.2](https://github.com/vicsharp-shibusa/kyna/milestone/5) was the beginning of building an analysis framework and the `Kyna.Analysis` library.
Simple in-memory charts and moving averages were composed.
[Phase 1.2](https://github.com/vicsharp-shibusa/kyna/milestone/5) was completed on February 19, 2024.

[Phase 1.3](https://github.com/vicsharp-shibusa/kyna/milestone/6) was a hardening iteration in which a few tweaks were made to the migrator.
This phase closed on February 21, 2024.

[Phase 1.4](https://github.com/vicsharp-shibusa/kyna/milestone/7) was the iteration in which back-testing got underway.
A model for "trends" was constructed, the `Kyna.Backtests.Cli` was built, and an initial, random "baseline test" was performed to set the standard for future signal tests.
The `Kyna.Cli` was constructed in this phase; this app is an orchestration app that calls the other CLI executables.
With this tool, instead of having to call the different apps separately, a user can call the core `kyna` app, as in `kyna import ...` or `kyna backtest ...`.
[Phase 1.4](https://github.com/vicsharp-shibusa/kyna/milestone/7) was completed on March 1, 2024.

[Phase 1.5](https://github.com/vicsharp-shibusa/kyna/milestone/8) was the phase in which we built signal capturing logic for all of the primary "candlestick reversal patterns."
Also in this phase, we added an alternative data importer using the Yahoo Finance API.
This importer, `YahooImporter` follows a simpler path than the `EodHdImporter` in that it does not separate the data collection from the migration - it combines the process and writes directly to the `financials` database; this is because we're not making direct API calls, but rather using a NuGet package. Plus, the Yahoo Finance API is free.

[Phase 1.6](https://github.com/vicsharp-shibusa/kyna/milestone/10) was a short phase in which we updated the Yahoo importer to hydrate our `entities` table.

[Phase 1.7](https://github.com/vicsharp-shibusa/kyna/milestone/11) was the stretch in which we added [polygon.io](https://polygon.io/) support. Polygon.io is a data provider for historical and real-time stock, options, indices, and crypto data.

The Kyna MVP project has come to a close. A lot of good work was done, many lessons were learned, and the project's output will be useful in the upcoming project(s).

### YouTube

This project's progress is being chronicled on a [YouTube channel Playlist](https://www.youtube.com/playlist?list=PLGw44r0iH8bayhAUZsMaK15Ny7--x8Mq_).
For more information about the thought processes behind various choices, please subscribe to the channel.

### Backtesting Approach

The backtesting approach will be as scientific as possible. Take an established hypothesis (there are many to choose from), construct a test, run the test, and measure the results. Refine and repeat. Abandon the idea or integrate it into your trading strategy. 

The backtesting engine must never be allowed to look forward. Given any pattern or criterion, the backtesting engine must make all decisions based only on information available at the time of the event analyzed. Even data such as the average height of a candle or average move per day must always be constrained to the information preceding the event being analyzed.

Many "indicators" or "patterns" evangelized in the finfluencer marketplace require that the market and/or price action be in a certain state (e.g., "trend") for the indicator to be valid. An example of this is the "Bullish Engulfing" candlestick pattern; for this pattern to be "valid," it must occur in a "downtrend." Determining "trend" will be an early priority within the project.

Determining success of an indicator (e.g., does the stock go up or down after {INSERT PATTERN HERE}?) is, of course, a top priority. To properly gauge the efficacy of a given indicator, a baseline probability of "success" is required. Early in the process, we will randomnly select price points on many charts and check the up/down ratio to use as our measuring stick. In other words, by throwing a dart at the chart, is it more likely to go up or down 10% from that point? 
