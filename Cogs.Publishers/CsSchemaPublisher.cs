﻿// Copyright (c) 2017 Colectica. All rights reserved
// See the LICENSE file in the project root for more information.
using Cogs.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cogs.Publishers
{
    public class CsSchemaPublisher
    {
        /// <summary>
        /// path to write output in
        /// </summary>
        public string TargetDirectory { get; set; }
        /// <summary>
        /// boolean to determine whether to replace existing or not
        /// </summary>
        public bool Overwrite { get; set; }

        public void Publish(CogsModel model)
        {
            if (TargetDirectory == null)
            {
                throw new InvalidOperationException("Target directory must be specified");
            }
            if (Overwrite && Directory.Exists(TargetDirectory))
            {
                Directory.Delete(TargetDirectory, true);
            }
            // TODO: if Overwrite is false and Directory.Exists(TargetDirectory)) throw an error and exit

            Directory.CreateDirectory(TargetDirectory);

            //get the project name
            var projName = "cogsBurger";

            // create xml header
            XDocument project = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup", new XElement("TargetFramework", "netstandard2.0"), 
                    new XElement("AssemblyName", projName), new XElement("RootNamespace", projName))));
            //create project file
            XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true };
            using (XmlWriter xw = XmlWriter.Create(Path.Combine(TargetDirectory, projName + ".csproj"), xws))
            {
                project.Save(xw);
            }
            // copy types file
            File.Copy(Path.Combine(Path.Combine(Path.Combine(TargetDirectory, ".."), ".."), "Types.cs"), Path.Combine(TargetDirectory, "Types.cs"));
            foreach (var item in model.ItemTypes.Concat(model.ReusableDataTypes))
            {
                // add class description using '$' for newline and '#' for tabs
                var newClass = new StringBuilder("using System;$using System.Collections.Generic;$$namespace " + projName +"${$#/// <summary>$#/// " + item.Description + "$#/// <summary>");
                newClass.Append("$#public ");
                // add abstract to class title if relevant
                if (item.IsAbstract) { newClass.Append("abstract "); }
                newClass.Append("class " + item.Name);
                // allow inheritance when relevant
                if (!String.IsNullOrWhiteSpace(item.ExtendsTypeName)) newClass.Append(" : " + item.ExtendsTypeName);
                newClass.Append("$#{");
                foreach(var prop in item.Properties)
                {
                    // create documentation for property
                    newClass.Append("$##/// <summary>$##/// " + prop.Description + "$##/// <summary>");
                    // create constraints
                    /*
                    if(prop.DataTypeName.Equals("string") || prop.DataTypeName.Equals("anyURI"))
                    {
                        newClass.Append("$##[StringLength(" + prop.MaxLength + ")]");
                        if(prop.MinLength != null) { newClass.Append("$##[StringLength.MinimumLength = " + prop.MinLength + "]"); }
                        if (prop.DataTypeName.Equals("string") && (prop.Enumerations != null || prop.Pattern != null))
                        {
                            // work with Enum and pattern
                            newClass.Append("$##[StringValidation(" + prop.Enumerations + ", " + prop.Pattern + ")]");
                        }
                    }else if(!prop.DataTypeName.Equals("boolean") && !prop.DataType.Equals("language") && !prop.DataTypeName.Equals("cogsDate"))
                    {
                        if (prop.MinInclusive != null || prop.MaxInclusive != null)
                        {
                            newClass.Append("$##[Range(" + prop.MinInclusive + ", " + prop.MaxInclusive + ")]");
                        }
                        if (prop.MinExclusive != null || prop.MaxExclusive != null)
                        {
                            newClass.Append("$##[ExclusiveRange(" + prop.MinExclusive + ", " + prop.MaxExclusive + ")]");
                        }
                    }*/
                    prop.DataTypeName = SetDataTypeName(prop.DataTypeName);
                    // if there can be at most one, create an instance variable
                    if (!prop.MaxCardinality.Equals("n") && Int32.Parse(prop.MaxCardinality) == 1)
                    {
                        newClass.Append("$##public " + prop.DataTypeName + " " + prop.Name + ";");
                    }
                    // otherwise, create a list object to allow multiple
                    else { newClass.Append("$##public List<" + prop.DataTypeName + "> " + prop.Name + " = new List<" + prop.DataTypeName + ">();"); }
                }
                newClass.Append("$#}$}");
                // write class to out folder
                File.WriteAllText(Path.Combine(TargetDirectory, item.Name + ".cs"), newClass.ToString().Replace("#", "    ").Replace("$", Environment.NewLine));
            }
        }

        // takes a data type name string and translates to a c# data structure representation name string
        private string SetDataTypeName(string dataType)
        {
            if (dataType.Equals("boolean")) { return "bool"; }
            if (dataType.Equals("integer")) { return "int"; }
            if (dataType.Equals("string")) { return "string"; }
            if (dataType.Equals("language")) { return "string"; }
            if (dataType.Equals("duration")) { return "TimeSpan"; }
            if (dataType.Equals("dateTime")) { return "DateTimeOffset"; }
            if (dataType.Equals("time")) { return "DateTimeOffset"; }
            if (dataType.Equals("date")) { return "DateTimeOffset"; }
            if (dataType.Equals("gYearMonth")) { return "Tuple<int, int>"; }
            if (dataType.Equals("gYear")) { return "int"; }
            if (dataType.Equals("gYearDay")) { return "Tuple<int, int>"; }
            if (dataType.Equals("gDay")) { return "int"; }
            if (dataType.Equals("gMonth")) { return "int"; }
            if (dataType.Equals("anyURI")) { return "Uri"; }
            if (dataType.Equals("nonPositiveInteger")) { return "int"; }
            if (dataType.Equals("negativeInteger")) { return "int"; }
            if (dataType.Equals("long")) { return "long"; }
            if (dataType.Equals("nonNegativeInteger")) { return "int"; }
            if (dataType.Equals("unsignedLong")) { return "ulong"; }
            if (dataType.Equals("positiveInteger")) { return "int"; }
            if (dataType.Equals("cogsDate")) { return "CogsDate"; }
            return dataType;
        }
    }
}