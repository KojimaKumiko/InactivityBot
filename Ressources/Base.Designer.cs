﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace InactivityBot.Ressources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Base {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Base() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("InactivityBot.Ressources.Base", typeof(Base).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} (tho I actually love coffee {1}).
        /// </summary>
        internal static string Author_Coffee {
            get {
                return ResourceManager.GetString("Author_Coffee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Undead Vampire.
        /// </summary>
        internal static string Author_Race {
            get {
                return ResourceManager.GetString("Author_Race", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The requested command was not found..
        /// </summary>
        internal static string Help_CommandNotFound {
            get {
                return ResourceManager.GetString("Help_CommandNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lists all executable commands for the current user! Specify a command to get more information about the command!.
        /// </summary>
        internal static string Help_Description {
            get {
                return ResourceManager.GetString("Help_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The command has no parameters.
        /// </summary>
        internal static string Help_NoParameters {
            get {
                return ResourceManager.GetString("Help_NoParameters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No summary available.
        /// </summary>
        internal static string Help_NoSummary {
            get {
                return ResourceManager.GetString("Help_NoSummary", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If you encounter any issue, either report them here: {0} or keep them to yourself. Whatever you prefer. (Tho obviouly, it would be better to report them).
        /// </summary>
        internal static string Problems {
            get {
                return ResourceManager.GetString("Problems", resourceCulture);
            }
        }
    }
}