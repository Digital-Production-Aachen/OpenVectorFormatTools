"""
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2023 Digital-Production-Aachen

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
"""

import os
import sys
import re

###### parameter section ######

# path where a file with the license header to use is found
header_path = "license_header.txt"

# dictionary with file types to process and in and out markers for block comments
file_types = {".cs": [r"/*", r"*/"], ".py": [r'"""', r'"""'], ".proto": [r"/*", r"*/"]}

# list with files / directories to ignore. please use '/' as directory seperator, will be replaced with system standard later.
# matching is done via regex against the relative file path.
exclude_list = [
    ".*/bin/.*",
    ".*/obj/.*",
    ".*/submodules/.*",
    ".*/.git/.*",
    ".*/.vs/.*",
    ".*/AssemblyInfo.cs"
]

###### end parameter section ######

with open(header_path, "r") as header_handle:
    license_header = header_handle.read()

license_header = license_header.strip("\n\r")

header_start = license_header.split("\n",1)[0]
header_end = license_header.rsplit("\n",1)[-1]

# check if run in CI - no replacing, just break at bad header
ci_mode = False
if (len(sys.argv) > 1 and sys.argv[1].lower() == "ci"):
    ci_mode = True

success = True

reg_patterns = []
for i in range(len(exclude_list)):
    exclude_list[i] = exclude_list[i].replace("/", os.sep.replace("\\", "\\\\"))
    reg_patterns.append(re.compile(exclude_list[i]))

for subdir, dirs, files in os.walk(r'.'):
    for filename in files:
        filepath = subdir + os.sep + filename
        
        # check if file should be excluded
        exclude = False
        for reg_pattern in reg_patterns:
            if reg_pattern.match(filepath):
                exclude = True
                break
        if exclude:
            continue

        # check if file is in the list of supported file types
        file_ext = "." + filepath.rsplit(".", 1)[-1]
        if file_ext in file_types:
                        
            with open(filepath, "r") as source_handle:
                source_file = source_handle.read()

            # check if correct license header is already present
            if source_file.find(license_header) >= 0:
                continue
            # if run inside of the CI - record bad header and continue, no replacing in CI
            elif ci_mode:
                print("[" + filepath + "]: no or invalid copyright header.")
                success = False
                continue            
            
            comment_marker_in = file_types[file_ext][0]
            comment_marker_out = file_types[file_ext][1]

            # chekc if header start / end mark is present 
            # if yes, replace header
            # if not, insert new header at begining of file
            start = source_file.find(header_start)
            end = source_file.find(header_end)
            if (start > end):
                sys.exit(-1)
            elif (start >= 0 and end > 0):
                print("[" + filepath + "]: replace header")
                source_start = source_file.split(header_start)[0]
                source_end = source_file.split(header_end, 1)[-1]
                new_source = source_start + license_header + source_end
            else:
                if (source_file.lower().find("copyright") >= 0):
                    print("[" + filepath + "]: found different copyright header - please remove")
                    sys.exit(1)
                print("[" + filepath + "]: insert new header")
                new_source = comment_marker_in + '\n' + license_header + '\n' + comment_marker_out +'\n\n' + source_file
            
            with open(filepath, "w") as source_handle:
                source_handle.write(new_source)
            continue 

if ci_mode and not success:
    print("Invalid copyright headers found.  Please run the " + sys.argv[0].rsplit(os.sep, 1)[-1] + " script locally to fix and commit again.")
    sys.exit(-1)
