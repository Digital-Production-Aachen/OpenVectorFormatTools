/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/

﻿namespace OpenVectorFormat.Plausibility
{
    /// <summary>Error Handling Mode for checker.</summary>
    public enum ErrorHandlingMode
    {
        /// <summary>Exception is thrown as soon as an check fails.</summary>
        THROWEXCEPTION = 0,

        /// <summary>If check fails, it is logged and the rest of the file is checked. Usefull to perform a full check and display all problems at once.</summary>
        LOGANDCONTINUE = 1,
    }
}
