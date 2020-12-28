using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror.SimpleWeb;

public class Client : MonoBehaviour
{
    private SimpleWebClient _webClient;

    private void Awake()
    {
        TcpConfig tcpConfig = new TcpConfig(true, 5000, 20000);
        _webClient = SimpleWebClient.Create(16*1024, 1000, tcpConfig);
        _webClient.onConnect += delegate
        {
            Debug.Log("Client connected");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Hello from client");
            _webClient.Send(new ArraySegment<byte>(bytes));
        };
        _webClient.onData += delegate(ArraySegment<byte> bytes)
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes.Array);
            Debug.Log("Received message : " + msg);
        };
        
        // connect
        UriBuilder uriBuilder = new UriBuilder()
        {
            Scheme = "wss",
            Host = "tylrpage.com",
            Port = Constants.GAME_PORT
        };
        _webClient.Connect(uriBuilder.Uri);
    }

    private void LateUpdate()
    {
        _webClient.ProcessMessageQueue(this);
    }
}
