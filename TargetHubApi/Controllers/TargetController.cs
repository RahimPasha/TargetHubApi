﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Threading.Tasks;
using System.Web.Http;
using TargetHubApi.Infrastructure;
using TargetHubApi.Models;
using System.Diagnostics;

namespace TargetHubApi.Controllers
{
    public class TargetController : ApiController
    {
        ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public HttpResponseMessage Download(string Identifier, int ID, string TargetName, string format)
        {
            //Check if the server is registered or not.
            if (Registered(Identifier, ID))
            {
                HttpResponseMessage result;
                string filePath = "";
                if (format == "dat")
                {
                    filePath = db.Targets.Where(t => t.Name == TargetName).FirstOrDefault().DatFilePath;
                }
                else if (format == "xml")
                {
                    filePath = db.Targets.Where(t => t.Name == TargetName).FirstOrDefault().XmlFilePath;
                }
                try
                {
                    var path = filePath;
                    result = new HttpResponseMessage(HttpStatusCode.OK);
                    var stream = new FileStream(path, FileMode.Open);
                    result.Content = new StreamContent(stream);
                    result.Content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");
                }
                catch (Exception e)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, e);
                }
                return result;
            }
            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Server is not registered!");
        }

        public async Task<HttpResponseMessage> Upload(string Identifier, int ID, string TargetName)
        {
            //Check if the server is registered or not.
            if (Registered(Identifier, ID))
            {
                // Check if the request contains multipart/form-data.
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                string root = HttpContext.Current.Server.MapPath("~/files");
                var provider = new MultipartFormDataStreamProvider(root);
                try
                {
                    // Read the form data.
                    await Request.Content.ReadAsMultipartAsync(provider);

                    string format = "";
                    // This illustrates how to get the file names.
                    foreach (MultipartFileData file in provider.FileData)
                    {
                        Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                        Trace.WriteLine("Server file path: " + file.LocalFileName);
                        format = file.Headers.ContentDisposition.FileName.ToLower().EndsWith("dat") ? "dat" : "Unknown";
                        format = file.Headers.ContentDisposition.FileName.ToLower().EndsWith("xml") ? "xml" : "Unknown";
                        if (format == "xml" || format == "dat")
                        {
                            if (db.Targets.Where(t => t.Name == TargetName).Count() == 0)
                            {
                                db.Targets.Add(new Target {
                                    Name = TargetName,
                                    DatFilePath = format == "xml" ? root + "/" + file.Headers.ContentDisposition.FileName : "",
                                    XmlFilePath = format == "dat" ? root + "/" + file.Headers.ContentDisposition.FileName : ""
                                });
                                db.SaveChanges();
                            }
                            else
                            {
                                if (format == "xml")
                                    db.Targets.Where(t => t.Name == TargetName).FirstOrDefault().XmlFilePath = file.Headers.ContentDisposition.FileName;
                                else
                                    db.Targets.Where(t => t.Name == TargetName).FirstOrDefault().DatFilePath = file.Headers.ContentDisposition.FileName;
                            }
                            db.SaveChanges();
                        }
                        else
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "only xml or dat types are valid!");
                        }
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                catch (System.Exception e)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
                }
            }
            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Server is not registered!");
        }
        private bool Registered(string identifier, int ID)
        {
            return db.Servers.Where(s => s.Id == ID && s.Identifier == identifier).Count() == 0 ? false : true;

        }
    }
}