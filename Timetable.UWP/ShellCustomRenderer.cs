﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(Shell), typeof(Timetable.UWP.ShellCustomRenderer))]
namespace Timetable.UWP
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ShellCustomRenderer : Microsoft.UI.Xaml.Controls.NavigationView, IVisualElementRenderer, IAppearanceObserver
    {
        internal static readonly Windows.UI.Color DefaultBackgroundColor = Windows.UI.Color.FromArgb(255, 3, 169, 244);
        internal static readonly Windows.UI.Color DefaultForegroundColor = Windows.UI.Colors.White;
        internal static readonly Windows.UI.Color DefaultTitleColor = Windows.UI.Colors.White;
        internal static readonly Windows.UI.Color DefaultUnselectedColor = Windows.UI.Color.FromArgb(180, 255, 255, 255);
        const string TogglePaneButton = "TogglePaneButton";
        const string NavigationViewBackButton = "NavigationViewBackButton";

        ShellItemCustomRenderer ItemRenderer { get; }

        /*
         * Note: DO NOT set FlyoutIsPresented
         * Make sure FlyoutIsPresented is always false
         * It will cause unexpected behaviour using this custom renderer
         * since we've made the NavigationView auto expand
         * 
         * Problem source Line 309 in Shell.cs
         * if (FlyoutIsPresented && FlyoutBehavior == FlyoutBehavior.Flyout)
				SetValueFromRenderer(FlyoutIsPresentedProperty, false);
         */

        public ShellCustomRenderer()
        {
            Xamarin.Forms.Shell.VerifyShellUWPFlagEnabled(nameof(ShellRenderer));
            IsBackEnabled = false;
            IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed;
            IsSettingsVisible = false;
            Content = ItemRenderer = CreateShellItemRenderer();
            MenuItemTemplateSelector = CreateShellFlyoutTemplateSelector();
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.Controls.NavigationView", "PaneClosing"))
                PaneClosing += (s, e) => OnPaneClosed();
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.Controls.NavigationView", "PaneOpening"))
                PaneOpening += (s, e) => OnPaneOpening();
            ItemInvoked += OnMenuItemInvoked;

            CompactModeThresholdWidth = 640;
            ExpandedModeThresholdWidth = 640;
            Resources["NavigationViewExpandedPaneBackground"] = Resources["SystemControlChromeHighAcrylicWindowMediumBrush"];
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdatePaneButtonColor(TogglePaneButton, !IsPaneOpen);
            UpdatePaneButtonColor(NavigationViewBackButton, !IsPaneOpen);
        }

        void OnPaneOpening()
        {
            UpdatePaneButtonColor(TogglePaneButton, false);
            UpdatePaneButtonColor(NavigationViewBackButton, false);

            IsPaneToggleButtonVisible = false;
            ItemRenderer.UpdateHeaderInsets();
        }

        void OnPaneClosed()
        {
            UpdatePaneButtonColor(TogglePaneButton, true);
            UpdatePaneButtonColor(NavigationViewBackButton, true);

            IsPaneToggleButtonVisible = true;
            ItemRenderer.UpdateHeaderInsets();
        }

        void OnMenuItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer?.DataContext as Element;
            if (item != null)
                ((IShellController)Element).OnFlyoutItemSelected(item);
        }

        #region IVisualElementRenderer

        event EventHandler<VisualElementChangedEventArgs> _elementChanged;

        event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
        {
            add { _elementChanged += value; }
            remove { _elementChanged -= value; }
        }

        FrameworkElement IVisualElementRenderer.ContainerElement => this;

        VisualElement IVisualElementRenderer.Element => Element;

        SizeRequest IVisualElementRenderer.GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            var constraint = new Windows.Foundation.Size(widthConstraint, heightConstraint);

            double oldWidth = Width;
            double oldHeight = Height;

            Height = double.NaN;
            Width = double.NaN;

            Measure(constraint);
            var result = new Size(Math.Ceiling(DesiredSize.Width), Math.Ceiling(DesiredSize.Height));

            Width = oldWidth;
            Height = oldHeight;

            return new SizeRequest(result);
        }

        public UIElement GetNativeElement() => null;

        public void Dispose()
        {
            SetElement(null);
        }

        public void SetElement(VisualElement element)
        {
            if (Element != null && element != null)
                throw new NotSupportedException("Reuse of the Shell Renderer is not supported");

            if (element != null)
            {
                Element = (Shell)element;
                Element.SizeChanged += OnElementSizeChanged;
                OnElementSet(Element);
                Element.PropertyChanged += OnElementPropertyChanged;
                ItemRenderer.SetShellContext(this);
                _elementChanged?.Invoke(this, new VisualElementChangedEventArgs(null, Element));

                Element.FlyoutIsPresented = false;
            }
            else if (Element != null)
            {
                Element.SizeChanged -= OnElementSizeChanged;
                Element.PropertyChanged -= OnElementPropertyChanged;
            }
        }

        #endregion IVisualElementRenderer

        protected internal Shell Element { get; set; }

        internal Shell Shell => Element;

        void OnElementSizeChanged(object sender, EventArgs e)
        {
            InvalidateMeasure();
        }

        protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Shell.CurrentItemProperty.PropertyName)
            {
                SwitchShellItem(Element.CurrentItem);
            }
        }

        protected virtual void OnElementSet(Shell shell)
        {
            var shr = CreateShellHeaderRenderer(shell);
            PaneCustomContent = shr;
            MenuItemsSource = IterateItems();
            SwitchShellItem(shell.CurrentItem, false);
            ((IShellController)shell).AddAppearanceObserver(this, shell);
        }

        IEnumerable<object> IterateItems()
        {
            var groups = ((IShellController)Shell).GenerateFlyoutGrouping();
            foreach (var group in groups)
            {
                if (group.Count > 0 && group != groups[0])
                {
                    yield return null; // Creates a separator
                }
                foreach (var item in group)
                {
                    yield return item;
                }
            }
        }

        void SwitchShellItem(ShellItem newItem, bool animate = true)
        {
            SelectedItem = newItem;
            ItemRenderer.NavigateToShellItem(newItem, animate);
        }

        void UpdatePaneButtonColor(string name, bool overrideColor)
        {
            var toggleButton = GetTemplateChild(name) as Control;
            if (toggleButton != null)
            {
                var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
                if (overrideColor)
                    toggleButton.Foreground = new SolidColorBrush(titleBar.ButtonForegroundColor.Value);
                else
                    toggleButton.ClearValue(Control.ForegroundProperty);
            }
        }

        #region IAppearanceObserver

        void IAppearanceObserver.OnAppearanceChanged(ShellAppearance appearance)
        {
            Windows.UI.Color backgroundColor = DefaultBackgroundColor;
            Windows.UI.Color titleColor = DefaultTitleColor;
            if (appearance != null)
            {
                if (!appearance.BackgroundColor.IsDefault)
                    backgroundColor = appearance.BackgroundColor.ToWindowsColor();
                if (!appearance.TitleColor.IsDefault)
                    titleColor = appearance.TitleColor.ToWindowsColor();
            }

            var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = titleBar.ButtonBackgroundColor = backgroundColor;
            titleBar.ForegroundColor = titleBar.ButtonForegroundColor = titleColor;
            UpdatePaneButtonColor(TogglePaneButton, !IsPaneOpen);
            UpdatePaneButtonColor(NavigationViewBackButton, !IsPaneOpen);
        }

        #endregion IAppearanceObserver

        public virtual ShellFlyoutTemplateSelector CreateShellFlyoutTemplateSelector() => new ShellFlyoutTemplateSelector();
        public virtual ShellHeaderRenderer CreateShellHeaderRenderer(Shell shell) => new ShellHeaderRenderer(shell);
        public virtual ShellItemCustomRenderer CreateShellItemRenderer() => new ShellItemCustomRenderer(this);
        public virtual ShellSectionCustomRenderer CreateShellSectionRenderer() => new ShellSectionCustomRenderer();
    }
}