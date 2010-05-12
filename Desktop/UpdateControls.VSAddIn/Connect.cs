/**********************************************************************
 * 
 * Update Controls .NET
 * Copyright 2010 Michael L Perry
 * Licensed under LGPL
 * 
 * http://updatecontrols.net
 * http://www.codeplex.com/updatecontrols/
 * 
 **********************************************************************/

using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.IO;

namespace UpdateControls.VSAddIn
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
        private readonly string MenuName = "Tools";

		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            using (TextWriter logWriter = OpenLogWriter())
            {
                logWriter.WriteLine("{0}: OnConnection( {1} )", DateTime.Now, connectMode);
                _applicationObject = (DTE2)application;
                _addInInstance = (AddIn)addInInst;
                if (connectMode == ext_ConnectMode.ext_cm_UISetup)
                {
                    logWriter.WriteLine("{0}: Connection mode is UISetup.", DateTime.Now);
                    object[] contextGUIDS = new object[] { };
                    Commands2 commands = (Commands2)_applicationObject.Commands;
                    string toolsMenuName;

                    try
                    {
                        //If you would like to move the command to a different menu, change the word "Tools" to the 
                        //  English version of the menu. This code will take the culture, append on the name of the menu
                        //  then add the command to that menu. You can find a list of all the top-level menus in the file
                        //  CommandBar.resx.
                        ResourceManager resourceManager = new ResourceManager("UpdateControls.VSAddIn.CommandBar", Assembly.GetExecutingAssembly());
                        CultureInfo cultureInfo = new System.Globalization.CultureInfo(_applicationObject.LocaleID);
                        string resourceName = String.Concat(cultureInfo.TwoLetterISOLanguageName, MenuName);
                        toolsMenuName = resourceManager.GetString(resourceName);
                        logWriter.WriteLine("{0}: Found a localized menu name.", DateTime.Now);
                    }
                    catch (Exception e)
                    {
                        logWriter.WriteLine("{0}: Failed to find a localized menu name. {1}", DateTime.Now, e.ToString());
                        //We tried to find a localized version of the word Tools, but one was not found.
                        //  Default to the en-US word, which may work for the current culture.
                        toolsMenuName = MenuName;
                    }
                    logWriter.WriteLine("{0}: Menu name is {1}.", DateTime.Now, toolsMenuName);

                    //Place the command on the menu.
                    //Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
                    Microsoft.VisualStudio.CommandBars.CommandBar menuBarCommandBar = ((Microsoft.VisualStudio.CommandBars.CommandBars)_applicationObject.CommandBars)["MenuBar"];

                    //Find the command bar on the MenuBar command bar:
                    CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
                    CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

                    //This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
                    //  just make sure you also update the QueryStatus/Exec method to include the new command names.
                    try
                    {
                        logWriter.WriteLine("{0}: Creating menu command.", DateTime.Now);
                        //Add a command to the Commands collection:
                        Command command = commands.AddNamedCommand2(
                            _addInInstance,
                            "GenerateIndependentProperties",
                            "Generate Independent Properties",
                            "Generate independent properties for selected fields.",
                            true,
                            59,
                            ref contextGUIDS,
                            (int)CalculateStatus(),
                            (int)vsCommandStyle.vsCommandStyleText,
                            vsCommandControlType.vsCommandControlTypeButton);

                        //Add a control for the command to the tools menu:
                        if ((command != null) && (toolsPopup != null))
                        {
                            logWriter.WriteLine("{0}: Adding menu command.", DateTime.Now);
                            command.AddControl(toolsPopup.CommandBar, 1);
                            if (BindCommandKeys(command, new string[] { "Ctrl+D" }, logWriter))
                                UnbindCommandKeys(command, logWriter);
                            BindCommandKeys(command, new string[] { "Ctrl+D, Ctrl+G", "Ctrl+D, G" }, logWriter);
                            logWriter.WriteLine("{0}: Success.", DateTime.Now);
                        }
                        else
                        {
                            logWriter.WriteLine("{0}: Could not locate command or popup.", DateTime.Now);
                        }
                    }
                    catch (System.ArgumentException e)
                    {
                        logWriter.WriteLine("{0}: Failed to add menu command. {1}", DateTime.Now, e.ToString());
                        //If we are here, then the exception is probably because a command with that name
                        //  already exists. If so there is no need to recreate the command and we can 
                        //  safely ignore the exception.
                    }
                }
            }
		}

        private TextWriter OpenLogWriter()
        {
            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string mallardsoft = Path.Combine(localApplicationData, "Michael L Perry");
            string updateControls = Path.Combine(mallardsoft, "Update Controls");
            Directory.CreateDirectory(updateControls);
            string logFile = Path.Combine(updateControls, "UpdateControls.VSAddIn.log");
            return new StreamWriter(logFile, true);
        }

        /// <summary>
        /// Binds given key to command.
        /// </summary>
        private bool BindCommandKeys(Command command, string[] keys, TextWriter logWriter)
        {
            object[] bindings;
            bindings = (object[])command.Bindings;

            // If our command already has a binding then it must've been set by the user
            // Don't overwire it then
            if (bindings.Length == 0)
            {
                string scope = GetKeyBindingScope();

                bindings = new object[keys.Length];
                for (int index = 0; index < keys.Length; ++index)
                {
                    string key = keys[index];
                    bindings[index] = (object)(scope + key);
                    logWriter.WriteLine("{0}: Binding {1}, {2}.", DateTime.Now, index, scope + key);
                }
                try
                {
                    command.Bindings = (object)bindings;
                    return true;
                }
                catch (Exception x)
                {
                    logWriter.WriteLine("{0}: Error while binding to keyboard commands: {1}", DateTime.Now, x.Message);
                }
            }
            else
            {
                logWriter.WriteLine("{0}: Bindings already exist : {1}.", DateTime.Now, bindings);
            }
            return false;
        }

        /// <summary>
        /// Removes key bindings from a command.
        /// </summary>
        private void UnbindCommandKeys(Command command, TextWriter logWriter)
        {
            object[] bindings;
            bindings = (object[])command.Bindings;

            bindings = new object[0];
            try
            {
                command.Bindings = (object)bindings;
            }
            catch (Exception x)
            {
                logWriter.WriteLine("{0}: Error while binding to keyboard commands: {1}", DateTime.Now, x.Message);
            }
        }

        /// <summary>
        /// Returns command shortcut binding. This value is localized but with no API to retrive it.
        /// Thus, we retrieve a well known built in command (Edit.Delete) which should have binding 
        /// set already. We then extract its scope and use that for all of our commands bindings.
        /// </summary>
        /// <returns></returns>
        private string GetKeyBindingScope()
        {
            return "Global::";
        }

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			if(neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
			{
                if (commandName == "UpdateControls.VSAddIn.Connect.GenerateIndependentProperties")
				{
                    status = CalculateStatus();
					return;
				}
			}
		}

        private vsCommandStatus CalculateStatus()
        {
            return vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
        }

        private void DisplayMessage(string message)
        {
            try
            {
                OutputWindow outputWin = _applicationObject.ToolWindows.OutputWindow;
                OutputWindowPane pane = outputWin.OutputWindowPanes.Item(1);
                pane.OutputString(message + "\n");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Error " + exc.Message);
            }
        }

		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{
			handled = false;
			if(executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			{
				if(commandName == "UpdateControls.VSAddIn.Connect.GenerateIndependentProperties")
				{
                    GenerateDynamicsCommand.GenerateIndependentProperties(_applicationObject);
					handled = true;
					return;
				}
			}
		}

        private DTE2 _applicationObject;
		private AddIn _addInInstance;
	}
}