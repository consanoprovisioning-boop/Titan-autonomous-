using Titan.ChiefOfStaff;

return await CommandRouter.RunAsync(args, defaultLaunch: OperatingSystem.IsWindows());
