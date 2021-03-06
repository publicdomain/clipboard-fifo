﻿// <copyright file="SettingsData.cs" company="PublicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace PublicDomain
{
    // Directives
    using System;
    using System.Drawing;

    /// <summary>
    /// Settings data.
    /// </summary>
    public class SettingsData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:PublicDomain.SettingsData"/> class.
        /// </summary>
        public SettingsData()
        {
            // Parameterless constructor for serialization to work
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:ClipboardUrlSaver.SettingsData"/> is always on top.
        /// </summary>
        /// <value><c>true</c> if always on top; otherwise, <c>false</c>.</value>
        public bool AlwaysOnTop { get; set; } = true;

        /*/// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MultilingualWordCounter.SettingsData"/> hides the close button.
        /// </summary>
        /// <value><c>true</c> if hide close button; otherwise, <c>false</c>.</value>
        public bool HideCloseButton { get; set; } = false;*/
    }
}
