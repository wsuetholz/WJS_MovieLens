using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Terminal.Gui;
using WJS_MovieLens.GUI;
using WJS_MovieLens.Services;

namespace WJS_MovieLens
{
    

    class Program
    {

        static void Main(string[] args)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();

            logger.Info("Program WJS_MovieLens Started");

            var gui = new MovieGUI(null);

            gui.Run();

            logger.Info("Program WJS_MovieLens Finished");
        }
    }
}
