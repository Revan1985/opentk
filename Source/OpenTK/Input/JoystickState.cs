﻿#region License
//
// JoystickState.cs
//
// Author:
//       Stefanos A. <stapostol@gmail.com>
//
// Copyright (c) 2006-2014 Stefanos Apostolopoulos
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

using System;
using System.Diagnostics;
using System.Text;

namespace OpenTK.Input
{
    /// <summary>
    /// Describes the current state of a <see cref="JoystickDevice"/>.
    /// </summary>
    public struct JoystickState : IEquatable<JoystickState>
    {
        // If we ever add more values to JoystickAxis or JoystickButton
        // then we'll need to increase these limits.
        internal const int MaxAxes = 10;
        internal const int MaxButtons = 32;

        const float ConversionFactor = 1.0f / (short.MaxValue + 0.5f);

        unsafe fixed short axes[MaxAxes];
        int buttons;
        int packet_number;
        bool is_connected;

        #region Public Members

        /// <summary>
        /// Gets a value between -1.0 and 1.0 representing the current offset of the specified  <see cref="JoystickAxis"/>.
        /// </summary>
        /// <returns>
        /// A value between -1.0 and 1.0 representing offset of the specified  <see cref="JoystickAxis"/>.
        /// If the specified axis does not exist, then the return value is 0.0. Use <see cref="Joystick.GetCapabilities"/>
        /// to query the number of available axes.
        /// </returns>
        /// <param name="axis">The <see cref="JoystickAxis"/> to query.</param>
        public float GetAxis(JoystickAxis axis)
        {
            return GetAxisRaw(axis) * ConversionFactor;
        }

        /// <summary>
        /// Gets the current <see cref="ButtonState"/> of the specified <see cref="JoystickButton"/>.
        /// </summary>
        /// <returns><see cref="ButtonState.Pressed"/> if the specified button is pressed; otherwise, <see cref="ButtonState.Released"/>.</returns>
        /// <param name="button">The <see cref="JoystickButton"/> to query.</param>
        public ButtonState GetButton(JoystickButton button)
        {
            return (buttons & (1 << (int)button)) != 0 ? ButtonState.Pressed : ButtonState.Released;
        }

        /// <summary>
        /// Gets a value indicating whether the specified <see cref="JoystickButton"/> is currently pressed.
        /// </summary>
        /// <returns>true if the specified button is pressed; otherwise, false.</returns>
        /// <param name="button">The <see cref="JoystickButton"/> to query.</param>
        public bool IsButtonDown(JoystickButton button)
        {
            return (buttons & (1 << (int)button)) != 0;
        }

        /// <summary>
        /// Gets a value indicating whether the specified <see cref="JoystickButton"/> is currently released.
        /// </summary>
        /// <returns>true if the specified button is released; otherwise, false.</returns>
        /// <param name="button">The <see cref="JoystickButton"/> to query.</param>
        public bool IsButtonUp(JoystickButton button)
        {
            return (buttons & (1 << (int)button)) == 0;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return is_connected; }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="OpenTK.Input.JoystickState"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="OpenTK.Input.JoystickState"/>.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < MaxAxes; i++)
            {
                sb.Append(" ");
                sb.Append(String.Format("{0:f4}", GetAxis(JoystickAxis.Axis0 + i)));
            }
            return String.Format(
                "{{Axes:{0}; Buttons: {1}; IsConnected: {2}}}",
                sb.ToString(),
                Convert.ToString((int)buttons, 2).PadLeft(16, '0'),
                IsConnected);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="OpenTK.Input.JoystickState"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode()
        {
            int hash = buttons.GetHashCode() ^ IsConnected.GetHashCode();
            for (int i = 0; i < MaxAxes; i++)
            {
                hash ^= GetAxisUnsafe(i).GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="OpenTK.Input.JoystickState"/>.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="OpenTK.Input.JoystickState"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current
        /// <see cref="OpenTK.Input.JoystickState"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return
                obj is JoystickState &&
                Equals((JoystickState)obj);
        }

        #endregion

        #region Internal Members

        internal int PacketNumber
        {
            get { return packet_number; }
        }

        internal short GetAxisRaw(JoystickAxis axis)
        {
            return GetAxisRaw((int)axis);
        }

        internal short GetAxisRaw(int axis)
        {
            short value = 0;
            if (axis >= 0 && axis < MaxAxes)
            {
                value = GetAxisUnsafe(axis);
            }
            else
            {
                Debug.Print("[Joystick] Invalid axis {0}", axis);
            }
            return value;
        }

        internal void SetAxis(JoystickAxis axis, short value)
        {
            int index = (int)axis;
            if (index < 0 || index >= MaxAxes)
                throw new ArgumentOutOfRangeException("axis");

            unsafe
            {
                fixed (short* paxes = axes)
                {
                    *(paxes + index) = value;
                }
            }
        }

        internal void SetButton(JoystickButton button, bool value)
        {
            int index = 1 << (int)button;
            if (value)
            {
                buttons |= index;
            }
            else
            {
                buttons &= ~index;
            }
        }

        internal void SetIsConnected(bool value)
        {
            is_connected = value;
        }

        internal void SetPacketNumber(int number)
        {
            packet_number = number;
        }

        #endregion

        #region Private Members

        short GetAxisUnsafe(int index)
        {
            unsafe
            {
                fixed (short* paxis = axes)
                {
                    return *(paxis + index);
                }
            }
        }

        #endregion

        #region IEquatable<JoystickState> Members

        /// <summary>
        /// Determines whether the specified <see cref="OpenTK.Input.JoystickState"/> is equal to the current <see cref="OpenTK.Input.JoystickState"/>.
        /// </summary>
        /// <param name="other">The <see cref="OpenTK.Input.JoystickState"/> to compare with the current <see cref="OpenTK.Input.JoystickState"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="OpenTK.Input.JoystickState"/> is equal to the current
        /// <see cref="OpenTK.Input.JoystickState"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(JoystickState other)
        {
            bool equals =
                buttons == other.buttons &&
                IsConnected == other.IsConnected;
            for (int i = 0; equals && i < MaxAxes; i++)
            {
                equals &= GetAxisUnsafe(i) == other.GetAxisUnsafe(i);
            }
            return equals;
        }

        #endregion
    }
}
