using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;

namespace WinUI.TableView.Tests;

internal static class UnoTestRunner
{
    internal static async Task RunAsync(UnitTestAppWindow window, bool exitAfterTests)
    {
        await Task.Yield();

        var results = new List<(string Name, Exception? Error)>();
        var testMethods = typeof(UnoTestRunner).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<TestClassAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttribute<TestMethodAttribute>() is not null)
                .Select(method => (Type: type, Method: method)))
            .OrderBy(test => test.Type.FullName, StringComparer.Ordinal)
            .ThenBy(test => test.Method.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var (type, method) in testMethods)
        {
            WriteMessage($"RUN {type.Name}.{method.Name}");
            Exception? error = null;

            try
            {
                if (method.GetParameters().Length != 0)
                {
                    throw new InvalidOperationException($"Test method {type.FullName}.{method.Name} must be parameterless.");
                }

                var instance = Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"Could not create test class {type.FullName}.");
                var result = method.Invoke(instance, null);

                if (result is Task task)
                {
                    await task;
                }
            }
            catch (Exception exception)
            {
                error = exception is TargetInvocationException { InnerException: not null } invocationException
                    ? invocationException.InnerException
                    : exception;
            }

            results.Add(($"{type.Name}.{method.Name}", error));
            var resultMessage = error is null
                ? $"PASS {type.Name}.{method.Name}"
                : $"FAIL {type.Name}.{method.Name}: {error}";
            WriteMessage(resultMessage);
        }

        var failures = results.Count(result => result.Error is not null);
        WriteMessage($"Uno tests complete: {results.Count - failures} passed, {failures} failed.");
        window.Content = CreateResultsView(results, failures);
        Environment.ExitCode = failures == 0 ? 0 : 1;

        if (exitAfterTests)
        {
            window.Close();
        }
    }

    internal static void WriteMessage(string message)
    {
        Console.WriteLine(message);
        Debug.WriteLine(message);
#if ANDROID
        Android.Util.Log.Info("UnoTests", message);
#endif
    }

    private static UIElement CreateResultsView(
        IReadOnlyList<(string Name, Exception? Error)> results,
        int failures)
    {
        var output = new StackPanel { Spacing = 4, Padding = new Thickness(16) };
        output.Children.Add(new TextBlock
        {
            Text = $"Uno tests complete: {results.Count - failures} passed, {failures} failed."
        });

        foreach (var (name, error) in results)
        {
            output.Children.Add(new TextBlock
            {
                Text = error is null ? $"PASS  {name}" : $"FAIL  {name}: {error.Message}",
                TextWrapping = TextWrapping.Wrap
            });
        }

        return new ScrollViewer { Content = output };
    }
}
