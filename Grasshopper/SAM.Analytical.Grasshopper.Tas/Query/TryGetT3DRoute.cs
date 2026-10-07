// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Maps the text of a Grasshopper "T3D Route" input to a <see cref="T3DRoute"/>.
        /// <para>
        /// <see cref="T3DRoute.Direct"/> is only ever the result of the exact word "Direct" (case-insensitive,
        /// surrounding white space ignored). Missing text and "GbXML" give <see cref="T3DRoute.GbXML"/>, the
        /// established route. Anything else is not recognised: <paramref name="t3DRoute"/> is still
        /// <see cref="T3DRoute.GbXML"/> and the method returns false so the caller can warn - an unreadable value
        /// must never switch the workflow to the new route.
        /// </para>
        /// </summary>
        /// <param name="text">The input text. Null or white space means "not set".</param>
        /// <param name="t3DRoute">The route to use; never <see cref="T3DRoute.Direct"/> unless the text says so.</param>
        /// <returns>False when <paramref name="text"/> is set but is neither "GbXML" nor "Direct".</returns>
        public static bool TryGetT3DRoute(string text, out T3DRoute t3DRoute)
        {
            t3DRoute = T3DRoute.GbXML;

            string value = text?.Trim();
            if (string.IsNullOrEmpty(value) || string.Equals(value, nameof(T3DRoute.GbXML), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, nameof(T3DRoute.Direct), StringComparison.OrdinalIgnoreCase))
            {
                t3DRoute = T3DRoute.Direct;
                return true;
            }

            return false;
        }
    }
}
