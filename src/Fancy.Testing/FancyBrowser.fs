namespace Fancy.Testing

    open System

    open Nancy.Testing

    type FancyBrowser(configureBootstrapper: ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator -> Unit) =
        inherit Browser(Action<ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator>(configureBootstrapper))