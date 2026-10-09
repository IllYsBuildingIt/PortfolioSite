using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.IDE;
using Microsoft.TemplateEngine.Abstractions;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using System.Linq;

namespace OnyxSpeedrun.Views;

public partial class WizardWindow : Window
{
    public WizardWindow()
    {
        InitializeComponent();
    }

    private async void NewProjectBtn_OnClick_(object? sender, RoutedEventArgs e)
    {
        newProjectView.IsVisible = true;
        welcomePageView.IsVisible = false;

        await GetThemTemplates();
    }

    private Bootstrapper CreateBootstrapper()
    {
        var builtIns = new List<(Type InterfaceType, IIdentifiedComponent Instance)>
        {
            (typeof(ITemplatePackageProviderFactory), new BuiltInTemplatePackageProviderFactory())
        };

        var host = new DefaultTemplateEngineHost(
            "OnyxSpeedRun",
            "1.0.0",
            null,
            builtIns);

        return new Bootstrapper(host, false, true);
    }

    private async Task GetThemTemplates()
    {
        using var bootstrapper = CreateBootstrapper();

        _templates = await bootstrapper.GetTemplatesAsync(
            CancellationToken.None);

        templateSearchBox.ItemsSource =
            _templates.Select(template => template.Name).ToList();
    }
    
    private IReadOnlyList<ITemplateInfo> _templates = [];

 

    private void backBtnNpv(object? sender, RoutedEventArgs e)
    {
        newProjectView.IsVisible = false;
        welcomePageView.IsVisible = true;
    }


}