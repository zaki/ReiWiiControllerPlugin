/*
 * Copyright (c) 2009, 3Di, Inc. (http://3di.jp/) and contributors.
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of 3Di, Inc., nor the name of the 3Di Viewer
 *       "Rei" project, nor the names of its contributors may be used to
 *       endorse or promote products derived from this software without
 *       specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY 3Di, Inc. AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL 3Di, Inc. OR THE
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using WiimoteLib;

namespace OpenViewer.Plugins
{
    public class WiiControllerPlugin : IManagerPlugin
    {
        #region Private Properties
        private bool isEnabled = true;
        private WiimoteCollection wiiCollection = null;
        private Guid activeWiimote = Guid.Empty;
        private bool up = false;
        private bool down = false;
        private int rumble = 0;
        #endregion

        public void Initialise(Viewer viewer)
        {
            // Mandatory initialization of Reference
            Reference = viewer.Reference;
            wiiCollection = new WiimoteCollection();
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///  This is the Initializer to run on every (re)start of the manager
        /// </summary>
        public void Initialize()
        {
            try
            {
                wiiCollection.FindAllWiimotes();
            }
            catch (WiimoteNotFoundException e)
            {
                Reference.Log.Warn("[WIICONTROLLER]: No wiimotes found: " + e.Message);
                return;
            }
            catch (Exception e)
            {
                Reference.Log.Warn("[WIICONTROLLER]: An exception occured: " + e.Message);
                return;
            }

            if (wiiCollection.Count == 0)
            {
                Reference.Log.Warn("[WIICONTROLLER]: No wiimotes found.");
            }
            else
            {
                // TODO: Add a way to select the active wiiMote
                activeWiimote = wiiCollection[0].ID;
                wiiCollection[0].WiimoteChanged += wm_WiimoteChanged;
                wiiCollection[0].WiimoteExtensionChanged += wm_WiimoteExtensionChanged;
                wiiCollection[0].SetReportType(InputReport.IRExtensionAccel, IRSensitivity.Maximum, true);
                try
                {
                    wiiCollection[0].Connect();
                    wiiCollection[0].SetLEDs(1);        // The active wiiMote will have the first LED set
                }
                catch (Exception e)
                {
                    Reference.Log.Warn("[WIICONTROLLER]: An exception occured: " + e.Message);
                    activeWiimote = Guid.Empty;
                    wiiCollection[0].WiimoteChanged -= wm_WiimoteChanged;
                    wiiCollection[0].WiimoteExtensionChanged -= wm_WiimoteExtensionChanged;
                }
            } 
        }

        /// <summary>
        /// Called every frame before rendering
        /// </summary>
        /// <param name="frame">Framecount</param>
        public void Update(uint frame)
        {
            if (wiiCollection == null || wiiCollection.Count == 0)
                return;
            
            if (frame % 100 == 0 && activeWiimote == System.Guid.Empty)
            {
                // TODO: Recheck wiimotes
            }

            if (activeWiimote != Guid.Empty)
            {
                if (isEnabled)
                {
                    if (up)
                        Reference.Viewer.Adapter.CallUserAvatarUp(true);
                    else
                        Reference.Viewer.Adapter.CallUserAvatarUp(false);
                    
                    if (down)
                        Reference.Viewer.Adapter.CallUserAvatarDown(true);
                    else
                        Reference.Viewer.Adapter.CallUserAvatarDown(false);
                }
                if (frame % 10 == 0)
                {
                    if (isEnabled)
                    {
                        wiiCollection[0].SetLEDs(1);
                    }
                    else
                    {
                        wiiCollection[0].SetLEDs(4);
                    }
                }

                if (rumble > 0)
                {
                    rumble -= 1;
                    wiiCollection[0].SetRumble(true);
                }
                else
                {
                    wiiCollection[0].SetRumble(false);
                }
            }
        }

        /// <summary>
        /// Custom scene drawing can happen here
        /// </summary>
        public void Draw()
        {
        }

        /// <summary>
        /// Clean up transient resources
        /// </summary>
        public void Cleanup()
        {
            activeWiimote = System.Guid.Empty;
            if (wiiCollection == null)
                return;

            if (wiiCollection.Count > 0)
            {
                wiiCollection[0].WiimoteChanged -= wm_WiimoteChanged;
                wiiCollection[0].WiimoteExtensionChanged -= wm_WiimoteExtensionChanged;
                wiiCollection[0].SetLEDs(0);
                wiiCollection[0].Disconnect();
                wiiCollection.Clear();
            }
        }

        void wm_WiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            if (e.Inserted)
                ((Wiimote)sender).SetReportType(InputReport.IRExtensionAccel, true);
            else
                ((Wiimote)sender).SetReportType(InputReport.IRAccel, true);
        }

        void wm_WiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            try
            {
                if (Reference.Viewer.AvatarManager.UserObject != null)
                {
                    if (((Wiimote)sender).ID == activeWiimote)
                    {
                        if (e.WiimoteState.NunchukState.Joystick.Y > 0.3f || e.WiimoteState.ButtonState.Up)
                            up = true;
                        else
                            up = false;
                        if (e.WiimoteState.NunchukState.Joystick.Y < -0.3f || e.WiimoteState.ButtonState.Down)
                            down = true;
                        else
                            down = false;

                        if (e.WiimoteState.NunchukState.Joystick.X < -0.3f || e.WiimoteState.ButtonState.Left)
                            Reference.Viewer.Adapter.CallUserAvatarLeft();
                        if (e.WiimoteState.NunchukState.Joystick.X > 0.3f || e.WiimoteState.ButtonState.Right)
                            Reference.Viewer.Adapter.CallUserAvatarRight();

                        if (e.WiimoteState.NunchukState.Z)
                        {
                            Reference.Viewer.Camera.MouseWheelAction(e.WiimoteState.NunchukState.AccelState.Values.Y / 10f);
                            //Reference.Viewer.Camera.SetDeltaFromMouse(e.WiimoteState.NunchukState.AccelState.Values.X, e.WiimoteState.NunchukState.AccelState.Values.Z);
                        }

                        if (e.WiimoteState.ButtonState.B)
                        {
                            Reference.Viewer.Camera.SetDeltaFromMouse(e.WiimoteState.AccelState.Values.X, e.WiimoteState.AccelState.Values.Z);
                        }

                        if (e.WiimoteState.NunchukState.C)
                        {
                            if (Reference.Viewer.Camera.CameraMode == ECameraMode.Build)
                                Reference.Viewer.Camera.SwitchMode(ECameraMode.Third);
                            else
                                Reference.Viewer.Camera.SwitchMode(ECameraMode.Build);
                        }

                        if (e.WiimoteState.ButtonState.A)
                        {
                            rumble = 10;
                        }

                        if (e.WiimoteState.ButtonState.Minus)
                        {
                            isEnabled = !isEnabled;
                        }

                        //Reference.Viewer.Adapter.CallDebugMessage("N:" + e.WiimoteState.NunchukState.AccelState.Values.ToString() +
                        //                                          "X:" + e.WiimoteState.AccelState.Values.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Reference.Log.Debug("[WIICONTROLLER]: An exception occured: " + ex.Message);
            }
        }

        public RefController Reference { get; set; }

        /// <summary>
        /// Returns the plugin version
        /// </summary>
        /// <returns>Plugin version in MAJOR.MINOR.REVISION.BUILD format</returns>
        public string Version { get { return ("1.0.0"); } }

        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns>Plugin name, eg MySQL User Provider</returns>
        public string Name { get { return ("WiiControllerPlugin"); } }

        /// <summary>
        /// Default-initialises the plugin
        /// </summary>
        public void Initialise()
        {
        }
    }
}