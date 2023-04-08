# LaunchDarkly Logging

This repo shows how to enable integrate the logging features within LaunchDarkly
with Serilog - or more precisely just the Microsoft.Extensions.Logging configuration.

A specific nuance here is that we need to access services is in the `IServiceCollection`
***before*** we would have normally called `builder.Build()` on the `WebApplicationBuilder` object
which creates the `IServiceProvider` from the `IServiceCollection`.

## Running the Application

You don't **have** to have a valid LaunchDarkly account set up, but some things will fail (with
logs) if you don't.  The value of the key is stored in `appsettings.json` in
the `LaunchDarklySdkKey` value.

I'm also writing logs to both the Console and [Seq](https://datalust.co/seq). Seq makes
the logs easier to review and explore.  The console has all of the log content, and can
be reviewed as well.

Assuming you can run Docker containers locally, the following command should work to get a
a Seq instance running locally:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

## The Big Concept

The important point in all of this is that during startup while you're adding services to
the `IServiceCollection`, you can call `BuildServiceProvider` to get an `IServiceProvider`
and then resolve services that you need.

But be careful!  This will create duplicates of any Singletons you've registered before
the `BuildServicesProvider` call!

## Configuring LaunchDarkly Logging

Once you've gotten this far, the configuration is simple and follows the process for all
other logs going through Serilog.  Most of the logs come from the `LaunchDarkly.Sdk` source
context, and the level is easily configured (LaunchDarkly is configured for `Debug` below):

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",
    "Override": {
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning",
      "LaunchDarkly": "Debug"
    }
  }
}
```

Here are notes from the [LaunchDarkly docs](https://docs.launchdarkly.com/sdk/features/logging#net-server-side) about the source contexts:

* `LaunchDarkly.Sdk`: general messages about the operation of the SDK client.
* `LaunchDarkly.Sdk.DataSource`: messages about how the SDK client receives feature flag data, such as if the connection to LaunchDarkly has been interrupted.
* `LaunchDarkly.Sdk.DataStore` (or more specific names like `LaunchDarkly.Sdk.DataStore.Redis`): messages about how the SDK client stores feature flag data, such as if you are using a database integration.
* `LaunchDarkly.Sdk.Evaluation`: messages about unusual conditions during feature flag evaluation, such as if a feature flag could not be evaluated because its configuration is invalid.
* `LaunchDarkly.Sdk.Events`: messages about analytics events, such as if event data could not be sent to LaunchDarkly due to a network problem.

