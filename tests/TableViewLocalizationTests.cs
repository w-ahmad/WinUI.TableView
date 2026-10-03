using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Linq;
using System.Reflection;
using WinUI.TableView.AutomationPeers;

namespace WinUI.TableView.Tests;

/// <summary>
/// DH-1944 — <see cref="TableViewLocalization.StringResolver"/>: the host application supplies the
/// TableView's strings (the automation control types and row names among them) from its own resources.
/// </summary>
[TestClass]
public class TableViewLocalizationTests
{
    [TestCleanup]
    public void ClearResolver() => TableViewLocalization.StringResolver = null;

    [UITestMethod]
    public void Keys_AreExactlyTheLocalizedStringProperties()
    {
        var properties = typeof(TableViewLocalizedStrings)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(properties, TableViewLocalization.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray(),
            "every string the TableView reads must be offered to the host's resolver under its resource name");
    }

    [UITestMethod]
    public void EveryKey_IsAskedOfTheResolver_AndItsAnswerIsUsed()
    {
        TableViewLocalization.StringResolver = key => "host:" + key;

        foreach (var property in typeof(TableViewLocalizedStrings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                     .Where(p => p.PropertyType == typeof(string)))
        {
            Assert.AreEqual("host:" + property.Name, property.GetValue(null), property.Name);
        }
    }

    [UITestMethod]
    public void A_null_or_empty_answer_keeps_the_librarys_own_value()
    {
        var own = TableViewLocalizedStrings.SortAscending;

        TableViewLocalization.StringResolver = _ => null;
        Assert.AreEqual(own, TableViewLocalizedStrings.SortAscending);

        TableViewLocalization.StringResolver = _ => string.Empty;
        Assert.AreEqual(own, TableViewLocalizedStrings.SortAscending);
    }

    [UITestMethod]
    public void A_resolver_that_throws_keeps_the_librarys_own_value()
    {
        var own = TableViewLocalizedStrings.CellControlType;

        TableViewLocalization.StringResolver = _ => throw new InvalidOperationException("host failure");

        Assert.AreEqual(own, TableViewLocalizedStrings.CellControlType);
    }

    [UITestMethod]
    public void The_resolver_is_asked_on_every_read_so_a_language_change_is_followed()
    {
        var language = "de";
        TableViewLocalization.StringResolver = key => key == nameof(TableViewLocalizedStrings.Row) ? "Zeile:" + language : null;
        Assert.AreEqual("Zeile:de", TableViewLocalizedStrings.Row);

        language = "fr";
        Assert.AreEqual("Zeile:fr", TableViewLocalizedStrings.Row);
    }

    [UITestMethod]
    public void FormatRowNumber_uses_the_hosts_format()
    {
        TableViewLocalization.StringResolver = key => key == nameof(TableViewLocalizedStrings.RowNumber) ? "Ligne {0}" : null;

        Assert.AreEqual("Ligne 3", TableViewLocalizedStrings.FormatRowNumber(3));
    }

    [UITestMethod]
    public void FormatRowNumber_falls_back_when_the_hosts_format_is_malformed()
    {
        var own = TableViewLocalizedStrings.FormatRowNumber(3);
        TableViewLocalization.StringResolver = key => key == nameof(TableViewLocalizedStrings.RowNumber) ? "Row {0" : null;

        Assert.AreEqual(own, TableViewLocalizedStrings.FormatRowNumber(3));
    }

    [UITestMethod]
    public void The_column_headers_localized_control_type_comes_from_the_resolver()
    {
        TableViewLocalization.StringResolver = key => key == nameof(TableViewLocalizedStrings.ColumnHeaderControlType) ? "Spaltenkopf" : null;

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new TableViewColumnHeader());

        Assert.IsInstanceOfType(peer, typeof(TableViewColumnHeaderAutomationPeer));
        Assert.AreEqual("Spaltenkopf", peer.GetLocalizedControlType());
    }

    [UITestMethod]
    public void A_row_peers_name_comes_from_the_resolver()
    {
        TableViewLocalization.StringResolver = key => key == nameof(TableViewLocalizedStrings.Row) ? "Baris" : null;

        Assert.AreEqual("Baris", new TableViewRowAutomationPeer(new TableViewRow()).GetName());
    }
}
