﻿using System;
using System.Web.Mvc;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.RenderingHelpers;

namespace SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.Models
{
    public class FileDataTaskDataValue : ITaskDataValue
    {
        public MvcHtmlString Render(HtmlHelper htmlHelper)
        {
            return htmlHelper.FileDataTaskDataValue(this);
        }

        public string Filename { get; set; }
        public Func<UrlHelper, string> GetUrl { get; set; }
    }
}