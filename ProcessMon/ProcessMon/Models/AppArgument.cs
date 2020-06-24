using System;

namespace ProcessMon.Models {

    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents an argument or switch which may be passed to the application at runtime.
    /// </summary>
    /// <remarks>
    /// This class can represent either a switch (boolean value; true or false) or a true argument.
    /// A true argument MUST match the following regular expression:
    /// <code>
    /// (MainArgument|AuxillaryArgument)[=]([\s\S]+)
    /// </code>
    /// </remarks>
    internal class AppArgument {

        /// <summary>
        /// The main argument the application shall respond to.
        /// </summary>
        public string MainArgument { get; set; }

        /// <summary>
        /// The auxillary argument (essentially an alternative argument). (OPTIONAL!)
        /// </summary>
        public string AuxillaryArgument { get; set; }

        /// <summary>
        /// The argument handler.
        /// </summary>
        public Action<string> ArgumentHandler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this argument is a switch or not.
        /// Default: true.
        /// </summary>
        /// <remarks>
        /// A switch argument only flips a boolean value to true or false.
        /// A "true" argument can handle more complex tasks and must be handled appropriately.
        /// </remarks>
        public bool IsSwitch { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this argument is case-sensitive or not.
        /// </summary>
        public bool IsCaseSensitive { get; set; } = false;

        /// <summary>
        /// Gets or sets the help text, which is displayed in the application's help menu.
        /// </summary>
        public string HelpText { get; set; }

        public string GetArgumentValue(string arg) {
            if (string.IsNullOrEmpty(arg)) throw new ArgumentNullException(nameof(arg));
            if (!GetArgumentRegex().IsMatch(arg)) throw new ArgumentException("Argument does not match regular expression!", nameof(arg));

            var splitArgs = arg.Split('=');
            var sBuilder = new StringBuilder();
            for (int i = 1; i < splitArgs.Length; i++) {
                // Start at second index in array, make sure to append '=' because .net has a habit of removing these chars.
                if (i > 1) sBuilder.Append('=');
                sBuilder.Append(splitArgs[i]);
            }

            return sBuilder.ToString();
        }

        StringComparison GetStringComparison() => IsCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        /// Creates and returns a regular expression that matches against the current argument (NOT SWITCH!)
        /// </summary>
        /// <returns>An instance of <see cref="System.Text.RegularExpressions.Regex"/></returns>
        Regex GetArgumentRegex() {
            return new Regex(
                // First match the main and aux args
                $@"({ MainArgument }{ (string.IsNullOrEmpty(AuxillaryArgument) ? string.Empty : $"|{ AuxillaryArgument }") })" +
                // Now match the rest of the argument
                @"[=]([\s\S]+)",
                IsCaseSensitive ? RegexOptions.Compiled | RegexOptions.CultureInvariant :
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
            );
        }

        #region Operator Overloads
        public static bool operator ==(string value, AppArgument x) {
            if (x.IsSwitch) {
                return value.Equals(x.MainArgument, x.GetStringComparison()) || value.Equals(x.AuxillaryArgument, x.GetStringComparison());
            }
            return x.GetArgumentRegex().IsMatch(value);
        }

        public static bool operator !=(string value, AppArgument x) => !(value == x);

        public static bool operator ==(AppArgument x, string y) => y == x;

        public static bool operator !=(AppArgument x, string y) => y != x;

        public static bool operator ==(AppArgument x, AppArgument y) => x.Equals(y);

        public static bool operator !=(AppArgument x, AppArgument y) => !x.Equals(y);
        #endregion

        /// <summary>
        /// Overrides the default Equals method.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><code>true</code> if the two objects are equal or not.</returns>
        public override bool Equals(object obj) {
            if (obj is string x)
                return x == this;

            return obj.GetHashCode() == GetHashCode();
        }

        /// <summary>
        /// Overridden method; calculates the hash code for this object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            unchecked {
                int hashResult = MainArgument?.GetHashCode() ?? 0;
                hashResult = (hashResult * 397) ^ AuxillaryArgument?.GetHashCode() ?? 0;
                hashResult = (hashResult * 397) ^ ArgumentHandler?.GetHashCode() ?? 0;
                hashResult = (hashResult * 397) ^ (IsSwitch ? 109 : 0);
                hashResult = (hashResult * 397) ^ (IsCaseSensitive ? 109 : 0);
                return hashResult;
            }
        }
    }
}
