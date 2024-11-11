using Hearthstone_Deck_Tracker;
using MyHsHelper.Properties;
using System;
using System.Windows;
using System.Windows.Controls;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace MyHsHelper.Logic
{
    /// <summary>
    /// Handles the functionality & math required to allow panel movement & permanency.
    /// </summary>
    public class InputMoveManager
    {
        private User32.MouseInput _mouseInput;
        private bool _selected = false;
        private StackPanel _StackPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputMoveManager"/> class.
        /// </summary>
        /// <param name="panel">The <see cref="StackPanel"/> object to move.</param>
        public InputMoveManager(StackPanel panel)
        {
            _StackPanel = panel;
        }

        private void MouseInputOnLmbDown(object sender, EventArgs eventArgs)
        {
            var pos = User32.GetMousePos();
            var _mousePos = new Point(pos.X, pos.Y);
            if (PointInsideControl(_mousePos, _StackPanel))
            {
                _selected = true;
            }
            else
            {
                _selected = false;
            }
        }

        private void MouseInputOnLmbUp(object sender, EventArgs eventArgs)
        {
            var pos = User32.GetMousePos();
            var _mousePos = new Point(pos.X, pos.Y);
            if (_selected)
            {
                var p = Core.OverlayCanvas.PointFromScreen(new Point(pos.X, pos.Y));
                if (pos.X > Core.OverlayCanvas.Width)
                {
                    Settings.Default.Left = p.X;
                }
                if (pos.Y > Core.OverlayCanvas.Height)
                {
                    Settings.Default.Top = p.Y;
                }
            }

            _selected = false;
        }

        private void MouseInputOnMouseMoved(object sender, EventArgs eventArgs)
        {
            if (_selected == false)
            {
                return;
            }

            var pos = User32.GetMousePos();
            var p = Core.OverlayCanvas.PointFromScreen(new Point(pos.X, pos.Y));
            if (pos.X > Core.OverlayCanvas.Width)
            {
                Canvas.SetLeft(_StackPanel, p.X);
            }
            if (pos.Y > Core.OverlayCanvas.Height)
            {
                Canvas.SetTop(_StackPanel, p.Y);
            }
        }

        /// <summary>
        /// Verify that the point the mouse is clicking upon, is inside the bounds of our control..
        /// </summary>
        /// <param name="p">The <see cref="Point" /> of the mouse click.</param>
        /// <param name="control">The <see cref="FrameworkElement" /> control.</param>
        /// <returns>True if inside the bounds of the control</returns>
        private bool PointInsideControl(Point p, FrameworkElement control)
        {
            var pos = control.PointFromScreen(p);
            return pos.X > 0 && pos.X < control.ActualWidth && pos.Y > 0 && pos.Y < control.ActualHeight;
        }

        public void Dispose()
        {
            _mouseInput?.Dispose();
            _mouseInput = null;
        }

        /// <summary>
        /// Toggles the panels visibility .
        /// </summary>
        /// <returns>True, If the panel is viable, else false</returns>
        public bool Toggle()
        {
            if (Hearthstone_Deck_Tracker.Core.Game.IsRunning && _mouseInput == null)
            {
                _mouseInput = new User32.MouseInput();
                _mouseInput.LmbDown += MouseInputOnLmbDown;
                _mouseInput.LmbUp += MouseInputOnLmbUp;
                _mouseInput.MouseMoved += MouseInputOnMouseMoved;
                return true;
            }
            Dispose();
            return false;
        }
    }
}