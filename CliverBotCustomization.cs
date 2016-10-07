//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//        27 February 2007
//Copyright: (C) 2007, Sergey Stoyan
//********************************************************************************************

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Web;
using System.Data;
using System.Web.Script.Serialization;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Net.Mail;
using Cliver;
using System.Configuration;
using System.Windows.Forms;
//using MySql.Data.MySqlClient;
using Cliver.Bot;
using Cliver.BotGui;
using Microsoft.Win32;
using System.Reflection;

namespace Cliver.BotCustomization
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            //Cliver.CrawlerHost.Linker.ResolveAssembly();
            main();
        }

        static void main()
        {
            //Cliver.Bot.Program.Run();//It is the entry when the app runs as a console app.
            Cliver.BotGui.Program.Run();//It is the entry when the app uses the default GUI.
        }
    }

    public class CustomBotGui : Cliver.BotGui.BotGui
    {
        override public string[] GetConfigControlNames()
        {
            return new string[] { "General", "Input", "Output", "Web", /*"Browser", "Spider",*/ "Proxy", "Log" };
        }

        override public Cliver.BaseForm GetToolsForm()
        {
            return null;
        }

        //override public Type GetBotThreadControlType()
        //{
        //    return typeof(IeRoutineBotThreadControl);
        //    //return typeof(WebRoutineBotThreadControl);
        //}
    }

    public class CustomBot : Cliver.Bot.Bot
    {
        new static public string GetAbout()
        {
            return @"WEB CRAWLER
Created: " + Cliver.Bot.Program.GetCustomizationCompiledTime().ToString() + @"
Developed by: www.cliversoft.com";
        }

        new static public void SessionCreating()
        {
            //InternetDateTime.CHECK_TEST_PERIOD_VALIDITY(2016, 10, 7);

            FileWriter.This.WriteHeader(
                "Name",
                "Company",
                "City",
                "State",
                "Site",
                "Phone",
                "Url"
            );
        }

        new static public void SessionClosing()
        {
        }

        override public void CycleBeginning()
        {
            //IR = new IeRoutine(((IeRoutineBotThreadControl)BotThreadControl.GetInstanceForThisThread()).Browser);
            //IR.UseCache = false;
            HR = new HttpRoutine();
        }

        //IeRoutine IR;

        HttpRoutine HR;

        public class CategoryItem : InputItem
        {
            readonly public string City;
            readonly public string State;

            override public void PROCESSOR(BotCycle bc)
            {
                CustomBot cb = (CustomBot)bc.Bot;
                //string url = "http://www.zillow.com/user/directory/LeaderboardsURLGenerator.htm?languageMask=&specialty=&proType=RealEstateAgent&searchTerm=" + City + " " + State;
                //if (!cb.HR.GetPage(url))
                //    throw new ProcessorException(ProcessorExceptionType.RESTORE_AS_NEW, "Could not get: " + url);
                //DataSifter.Capture c = category1.Parse(cb.HR.HtmlResult);
                //url = c.ValueOf("Url");
                string url = "http://www.zillow.com/" + City + "-" + State + "/real-estate-agent-reviews/?teamPlayer=False";
                cb.process_category(url);
            }
        }
        //static DataSifter.Parser category1 = new DataSifter.Parser("company.fltr");

        void process_category(string url)
        {
            if (!HR.GetPage(url))
                throw new ProcessorException(ProcessorExceptionType.RESTORE_AS_NEW, "Could not get: " + url);

            DataSifter.Capture c = category.Parse(HR.HtmlResult);

            string np = c.ValueOf("NextPage");
            if (np != null)
                BotCycle.Add(new CategoryNextPageItem(Regex.Replace(url, @"&page=\d+", "") + @"&page=" + int.Parse(np)));

            string[] us = Spider.GetAbsoluteUrls(c.ValuesOf("Url"), url, HR.HtmlResult);
            foreach (string u in us)
                BotCycle.Add(new CompanyItem(u));
        }

        static DataSifter.Parser category = new DataSifter.Parser("category.fltr");

        public class CategoryNextPageItem : InputItem
        {
            readonly public string Url;

            public CategoryNextPageItem(string url)
            {
                Url = url;
            }

            override public void PROCESSOR(BotCycle bc)
            {
                CustomBot cb = (CustomBot)bc.Bot;
                cb.process_category(Url);
            }
        }

        public class CompanyItem : InputItem
        {
            readonly public string Url;

            public CompanyItem(string url)
            {
                Url = url;
            }

            override public void PROCESSOR(BotCycle bc)
            {
                CustomBot cb = (CustomBot)bc.Bot;
               if (!cb.HR.GetPage(Url))
                    throw new ProcessorException(ProcessorExceptionType.RESTORE_AS_NEW, "Could not get: " + Url);

                DataSifter.Capture c = CustomBot.company.Parse(cb.HR.HtmlResult);

                FileWriter.This.PrepareAndWriteHtmlLineWithHeader(
                    "Name", c.ValueOf("Name"),
                    "Company", c.ValueOf("Company"),
                    "City", c.ValueOf("Locality"),
                    "State", c.ValueOf("Region"),
                    "Site", c.ValueOf("Website"),
                    "Phone", c.ValueOf("Phone"),
                    "Url", Url
                    );
            }
        }
        static DataSifter.Parser company = new DataSifter.Parser("company.fltr");
    }
}