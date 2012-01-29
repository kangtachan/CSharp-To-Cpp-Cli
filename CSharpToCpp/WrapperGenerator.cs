﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using DotLiquid;
using CSharpToCpp.Gen;

namespace CSharpToCpp
{
    public class WrapperGenerator
    {
        String OutPath { get; set; }
        String DynamicLib { get; set; }

        Dictionary<Type, GenClass> _GeneratedList = new Dictionary<Type, GenClass>();
        Dictionary<String, Template> _TemplateList = new Dictionary<string, Template>();

        public WrapperGenerator(string outPath, string lib)
        {
            OutPath = outPath;
            DynamicLib = lib;

            Directory.CreateDirectory(outPath + "\\include");
            Directory.CreateDirectory(outPath + "\\code");

            Template.RegisterTag<ParametersTag>("parameters");
            Template.RegisterTag<ParametersCallTag>("parametersCall");
            Template.RegisterTag<FunctionCallTag>("functionCall");

            GenerateTemplate(null, "NativeObjectInterface.txt", OutPath + "\\include\\NativeObjectI.h");
        }

        public void Generate(Type type)
        {
            if (!type.IsClass && !type.IsInterface)
                return;

            if (_GeneratedList.ContainsKey(type))
                return;

            GenClass gc = new GenClass(type);
            _GeneratedList.Add(type, gc);

            if (type.IsInterface)
            {
                GenerateTemplate(gc, "ProxyInterface.txt", OutPath + "\\include\\" + type.Name + "I.h");
                GenerateTemplate(gc, "ProxyHeader.txt", OutPath + "\\code\\" + type.Name + ".h");
                GenerateTemplate(gc, "ProxyBody.txt", OutPath + "\\code\\" + type.Name + ".cpp");
            }
            else
            {
                GenerateTemplate(gc, "WrapperInterface.txt", OutPath+"\\include\\" + type.Name + "I.h");
                GenerateTemplate(gc, "WrapperHeader.txt", OutPath + "\\code\\" + type.Name + ".h");
                GenerateTemplate(gc, "WrapperBody.txt", OutPath + "\\code\\" + type.Name + ".cpp");
            }

            foreach (var usedType in gc.UsedTypes)
                Generate(usedType);
        }

        private void GenerateTemplate(GenClass gc, string templateName, string outPath)
        {
            if (!_TemplateList.ContainsKey(templateName))
            {
                String tempContent = File.ReadAllText("Templates\\" + templateName);
                _TemplateList.Add(templateName, Template.Parse(tempContent));
            }

            String output = _TemplateList[templateName].Render(Hash.FromAnonymousObject(new { c = gc }));

            using (StreamWriter outFile = new StreamWriter(outPath))
            {
                outFile.Write(output);
            }
        }

        public IEnumerable<Type> GetTypesWith<TAttribute>() where TAttribute : System.Attribute
        {
            return from t in Assembly.LoadFrom(DynamicLib).GetTypes()
                   where t.IsDefined(typeof(TAttribute), false)
                   select t;
        }   
    }
}
