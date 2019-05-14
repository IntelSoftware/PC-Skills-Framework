// Copyright 2019 Intel Corporation.
//
// The source code, information and material ("Material") contained herein is owned by
// Intel Corporation or its suppliers or licensors, and title to such Material remains
// with Intel Corporation or its suppliers or licensors. The Material contains
// proprietary information of Intel or its suppliers and licensors. The Material is
// protected by worldwide copyright laws and treaty provisions. No part of the
// Material may be used, copied, reproduced, modified, published, uploaded, posted,
// transmitted, distributed or disclosed in any way without Intel's prior express
// written permission. No license under any patent, copyright or other intellectual
// property rights in the Material is granted to or conferred upon you, either
// expressly, by implication, inducement, estoppel or otherwise. Any license under
// such intellectual property rights must be express and approved by Intel in writing.
//
// Unless otherwise agreed by Intel in writing, you may not remove or alter this
// notice or any other notice embedded in Materials by Intel or Intel's suppliers or
// licensors in any way.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// list of strings to hold the actual name and confused names. First element is the actual name
using ConfusionList = System.Collections.Generic.List<string>;

// main or master list of lists that holds all the lists
using MainConfusionList = System.Collections.Generic.List<System.Collections.Generic.List<string>>;

// example data
// actual name: excel
// confused names: Exide, xl, Pixie, Exiding, Exist, Exciting, ...
// if any of the confused names appears, we should convert it to the actual name

// we want to create a list with first element as "excel" and confused names added as subsequent elements to
// the list.
// the verification request function will provide input string.
// we have to look for that string in our list and if it matches any of the elements present in the list
// we have to return the first element of the list, which is the actual name we were expecting in the first place.

// we need methods that create the master list, add multiple/single element to desired sub-lists
// another method will handle the verification request.

namespace PCSkillExample
{
    class ConfusionMatrix
    {
        // Mster List that contains all the lists
        private MainConfusionList MainList;

        // get text from file and return it as array of strings
        private string[] GetLinesFromFile(string fileName)
        {
            string[] lines = null;
            var list = new List<string>();
            if (File.Exists(fileName))
            {
                var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }

                lines = list.ToArray();
            }
            return lines;

        }

        // make a new list from a text file where first word in a line is the actual word and the remaining words are
        // the confused words, all seperated by comma.
        public bool MakeList(string fileName)
        {
            string[] lines = GetLinesFromFile(fileName);

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    string[] tokens = line.Split(',');
                    int count = tokens.Length;
                    // remove any unwanted spaces from front & back
                    for (int i = 0; i < count; i++)
                    {
                        tokens[i] = tokens[i].Trim();
                    }
                    // check if the current first element is already present in the master list
                    ConfusionList cList = GetConfusionListFromMasterList(tokens[0]);
                    if (cList == null)
                    {
                        // if current token is not in master list, make new confusion list & add it to master list
                        cList = MakeList(tokens);
                        AddToMasterList(cList);
                    }
                    else
                    {
                        // list exists already, add to it
                        count = tokens.Length;
                        for (int i = 1; i < count; i++)
                        {
                            // don't add this token if it already exists in the list
                            if (!cList.Contains(tokens[i].ToLower()))
                            {
                                cList.Add(tokens[i]);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }


        // add elements from names array to pre-existing cList 
        public bool MakeList(ConfusionList cList, params string[] names)
        {
            if (cList == null || cList.Count == 0 || names.GetLength(0) == 0)
            {
                return false;
            }
            foreach (string item in names)
            {
                // don't add if it already exists in the list
                if (!cList.Contains(item))
                {
                    cList.Add(item.ToLower());
                }
            }

            return true;
        }

        // create a new list from an array of names where first element is the mainName 
        // & remaining elements are part of confusion list
        public ConfusionList MakeList(params string[] names)
        {
            ConfusionList cList = new ConfusionList();

            foreach (string item in names)
            {
                // don't add if it already exists in the list
                if (!cList.Contains(item))
                {
                    cList.Add(item.ToLower());
                }
            }

            return cList;
        }

        // make new list. mainName is the main element, names is array of confusion list
        public ConfusionList MakeList(string mainName, params string[] names)
        {
            ConfusionList cList = new ConfusionList();

            cList.Add(mainName);
            foreach (string item in names)
            {
                // don't add if it already exists in the list
                if (!cList.Contains(item))
                {
                    cList.Add(item.ToLower());
                }
            }
            return cList;
        }

        // add confusion list to main list
        public bool AddToMasterList(ConfusionList cList)
        {
            // check if input list is valid
            if (cList == null || cList.Count == 0)
            {
                return false;
            }
            // check if any of the lists already contain the main element at index 0
            if (MainList != null && MainList.Count != 0)
            {
                foreach (var list in MainList)
                {
                    if (list.Contains(cList[0]))
                    {
                        // list for main element already exists, return failure
                        return false;
                    }
                }
            }

            // create new master list if not already present
            if (MainList == null)
            {
                MainList = new MainConfusionList();
            }

            // add current list to master list of lists
            MainList.Add(cList);
            return true;
        }

        // get a confusion list from master list, mainName is first element of the confusion list
        public ConfusionList GetConfusionListFromMasterList(string mainName)
        {
            // check if master list is valid
            if (MainList == null || MainList.Count == 0)
            {
                return null;
            }

            // check if mainName exists as the first element of any of the lists
            foreach (ConfusionList cList in MainList)
            {
                if (string.Equals(cList[0], mainName, StringComparison.OrdinalIgnoreCase))
                {
                    return cList;   // desired list found
                }

            }
            return null;
        }

        // add single element (name) to appropriate list (denoted by mainName) in master list
        public bool AddToMasterList(string mainName, string name)
        {
            // check if master list is valid
            if (MainList == null || MainList.Count == 0)
            {
                return false;
            }
            // check if mainName exists as the first element of any of the lists
            foreach (ConfusionList cList in MainList)
            {
                if (string.Equals(cList[0], mainName, StringComparison.OrdinalIgnoreCase))
                {
                    // desired list found, add the name
                    cList.Add(name);
                    return true;      // quit the loop as the job is done already
                }

            }

            return false;
        }


        // get correct name for a confused name. return original string if not found
        public string GetCorrectName(string name)
        {
            if (string.IsNullOrEmpty(name) || MainList == null || MainList.Count == 0)
            {
                return name;    // we got nothing, return the original name
            }
            // check if any of the lists already contain the main element at index 0
            // prepare LINQ to get first element from the list of master list that contains name
            IEnumerable<string> correctName = from list in MainList
                                              where list.Contains(name.ToLower())
                                              select list[0];
            // execute LINQ and return value if valid value exists
            if (!string.IsNullOrEmpty(correctName.FirstOrDefault<string>()))
            {
                return correctName.FirstOrDefault<string>();
            }
            return name;    // we couldn't find anything, return the original name
        }

        // get array of correct names for an array of confused names. return original names if not found
        public string[] GetCorrectName(string[] names)
        {
            // prepare lambda to execute on each element of names
            Func<string, string> correctName = str => GetCorrectName(str);
            // execute lambda, convert to an array of strings and return converted names
            return names.Select(correctName).ToArray<string>();
        }

    }
}
