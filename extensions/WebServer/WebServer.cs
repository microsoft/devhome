// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace WebServer;

public class WebServer : IDisposable
{
    private readonly HttpListener _listener;

    private readonly string _port;

    private readonly string _webcontentPath;

    private readonly Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, bool>> _routeHandlers = new();

    public string Port => _port;

    public WebServer(string webcontentPath)
    {
        _port = NextFreePort().ToString(null, CultureInfo.InvariantCulture);
        _webcontentPath = webcontentPath;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        Receive();
    }

    // Stop the server
    public void Stop()
    {
        _listener.Stop();
    }

    // Start listening for incoming requests
    private void Receive()
    {
        _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
    }

    // Handle incoming requests
    private void ListenerCallback(IAsyncResult result)
    {
        if (_listener.IsListening)
        {
            var context = _listener.EndGetContext(result);
            var request = context.Request;
            var response = context.Response;

            var filePath = PathCombine(_webcontentPath, request.RawUrl!).Replace("%20", " ");
            Debug.WriteLine($"Requested file path: {filePath}");

            if (_routeHandlers.TryGetValue(request.RawUrl!, out var value))
            {
                var handler = value;
                if (handler != null)
                {
                    if (handler(request, response))
                    {
                        response.Close();
                        Receive();
                        return;
                    }
                }
            }

            // If file path doesn't exist, return 404
            if (!File.Exists(filePath))
            {
                response.StatusCode = 404;
                response.StatusDescription = "Not Found";
                response.Close();
                Receive();
                return;
            }

            // Set the Content-Type header based on the file extension
            var contentType = GetContentType(filePath);
            response.ContentType = contentType;

            var buffer = File.ReadAllBytes(filePath);
            response.ContentLength64 = buffer.Length;
            Stream st = response.OutputStream;
            st.Write(buffer, 0, buffer.Length);

            response.Close();

            Receive();
        }
    }

    // Combine paths, handling the case where the second path is rooted
    private string PathCombine(string path1, string path2)
    {
        if (Path.IsPathRooted(path2))
        {
            path2 = path2.TrimStart(Path.DirectorySeparatorChar);
            path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);
        }

        return Path.Combine(path1, path2);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        Stop();
    }

    // Check if a port is free
    public bool IsFree(int port)
    {
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] listeners = properties.GetActiveTcpListeners();
        var openPorts = listeners.Select(item => item.Port).ToArray<int>();
        return openPorts.All(openPort => openPort != port);
    }

    // Find the next free port
    public int NextFreePort(int port = 0)
    {
        port = (port > 0) ? port : new Random().Next(1, 65535);
        while (!IsFree(port))
        {
            port += 1;
        }

        return port;
    }

    // Register a route that calls a passed in function
    public void RegisterRouteHandler(string route, Func<HttpListenerRequest, HttpListenerResponse, bool> function)
    {
        _routeHandlers.Add(route, function);
    }

    // Helper method to determine the Content-Type based on the file extension
    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream",
        };
    }
}
