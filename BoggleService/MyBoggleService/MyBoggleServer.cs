using BoggleService.Controllers;
using CustomNetworking;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using BoggleService.Models;
using System.Net.Sockets;
using static BoggleService.Controllers.BoggleController;

namespace MyBoggleService
{
    class MyBoggleServer
    {
        static void Main(string[] args)
        {
            new MyBoggleServer(60000);

            // This is our way of preventing the main thread from            
            // exiting while the server is in use            
            Console.ReadLine();
        }

        // Listens for incoming connection requests
        private StringSocketListener server;

        //controller to handle requests
        private BoggleController controller;

        /// <summary>
        /// Creates a BoggleServer that listens for connection requests on port 60000.
        /// </summary>
        public MyBoggleServer(int port)
        {
            //create one controller for this server
            controller = new BoggleController();

            //listens for incoming connection requests
            server = new StringSocketListener(60000, new System.Text.UTF8Encoding());

            server.Start();

            // Ask the server to call ConnectionRequested at some point in the future when 
            // a connection request arrives.  It could be a very long time until this happens.
            // The waiting and the calling will happen on another thread.  BeginAcceptSocket 
            // returns immediately, and the constructor returns to Main.
            server.BeginAcceptStringSocket(ConnectionRequested, null);
        }

        private void ConnectionRequested(StringSocket socket, object payload)
        {

            // We ask the server to listen for another connection request.  As before, this
            // will happen on another thread.
            server.BeginAcceptStringSocket(ConnectionRequested, null);

            //create a ClientConnection to deal with this socket
            new ClientConnection(socket, controller);
        }
    }

    /// <summary>
    /// Represents a connection with a remote client.  Takes care of receiving and sending
    /// information to that client according to the protocol.
    /// </summary>
    class ClientConnection
    {

        // The socket through which we communicate with the remote client
        private StringSocket socket;

        //the controller for every ClientConnection
        private BoggleController controller;

        //the length of the body provided by the client
        private int IncomingContentLength;

        //game id from the url of the request
        //only relevant for play word or get status requests
        private string GameID;



        /// <summary>
        /// Creates a ClientConnection from the socket, then begins communicating with it.
        /// </summary>
        public ClientConnection(StringSocket s, BoggleController c)
        {
            // Record the socket and controller and clear incoming
            socket = s;
            controller = c;

            // Ask the socket to call MessageReceive when it recieves a complete line from the client
            socket.BeginReceive(MessageRecieved, null);
        }

        /// <summary>
        /// Called when some data has been received.
        /// </summary>
        private void MessageRecieved(string s, object payload)
        {
            //figure out what the client's request is
            Regex RegisterRequest = new Regex("^POST /BoggleService/users");
            Regex JoinRequest = new Regex("^POST /BoggleService/games");
            Regex CancelRequest = new Regex("^PUT /BoggleService/games HTTP");
            Regex PlayWord = new Regex("^PUT /BoggleService/games/");
            Regex StatusRequest = new Regex("^GET /BoggleService/games");
            Regex ContainsTrue = new Regex("/True");
            if (RegisterRequest.IsMatch(s))
            {
                //if request is to register, ask socket to find the body
                socket.BeginReceive(FindBody, "register");
            }
            else if (JoinRequest.IsMatch(s))
            {
                //if request is to join a game, ask socket to find the body
                socket.BeginReceive(FindBody, "join");
            }
            else if (CancelRequest.IsMatch(s))
            {
                socket.BeginReceive(FindBody, "cancel");
            }
            else if (PlayWord.IsMatch(s))
            {
                //if request is to play a word, store the url from this line of the request
                GetGameID(s);
                //then ask the socket to listen for the body of request
                socket.BeginReceive(FindBody, "play");
            }
            else if(StatusRequest.IsMatch(s))
            {
                //if the request is to get game status, store the url from this line
                GetGameID(s);
                //then check if the brief paramter of this line is true or false
                if (ContainsTrue.IsMatch(s))
                {
                    //handle the request with the controller
                    HandleRequest(null, "true");
                }
                else
                {
                    //handle the request with the controller
                    HandleRequest(null, "false");
                }
            }
            //if the request is none of these, we've messed something up
            else
            {
                //ask socket to respond with internal error code
                Respond("error");
            }
        }

        /// <summary>
        /// Takes the first line of the request and sets game id given in the url
        /// </summary>
        private void GetGameID(string s)
        {
            char[] seperators = new char[2];
            seperators[0] = '/';
            seperators[1] = ' ';

            GameID = s.Split(seperators)[4];
        }

        /// <summary>
        /// continues to ask the socket for the next line until it finds the content length line,
        /// stores content length, then finds the body, then calls handle request with the body.
        /// </summary>
        private void FindBody(string s, object RequestType)
        {
            Regex contentLength = new Regex("Content-Length");
            //when socket recieves length of the body, store this info for later
            if (s != null && contentLength.IsMatch(s))
            {
                string[] split = s.Split(' ');
                IncomingContentLength = int.Parse(split[1]);
                socket.BeginReceive(FindBody, RequestType);

            }
            //when socket gets to blank line, next will come content of body
            //send body to the handle method
            else if (s == "\r")
            {
                socket.BeginReceive(HandleRequest, RequestType, IncomingContentLength);
            }
            //if socket does not recieve conent length or blank line, call this method again
            else
            {
                socket.BeginReceive(FindBody, RequestType);
            }
        }

        /// <summary>
        /// Contacts controller to get a response and then gives the response to Respond method
        /// s is the body of the request
        /// payload is a string identifying the request
        /// </summary>
        private void HandleRequest(string s, object payload)
        {
            try
            {
                //figure out the appropriate controller call
                if (payload == "join")
                {
                    dynamic body = JsonConvert.DeserializeObject<JoinGameInput>(s);
                    string ResponseBody = JsonConvert.SerializeObject(controller.PostJoinGame(body));
                    Respond(ResponseBody);
                }
                else if (payload == "register")
                {
                    string NickName = JsonConvert.DeserializeObject<string>(s);
                    string UserToken = controller.PostRegister(NickName);
                    string Body = JsonConvert.SerializeObject(UserToken);
                    Respond(Body);
                }
                else if (payload == "cancel")
                {
                    string UserToken = JsonConvert.DeserializeObject<string>(s);
                    controller.PutCancelGame(UserToken);
                    Respond("NoContent");
                }
                else if (payload == "play")
                {
                    PlayWordInput body = JsonConvert.DeserializeObject<PlayWordInput>(s);
                    string score = controller.PutPlayWord(GameID, body).ToString();
                    Respond(score);
                }
                else if (payload == "true")
                {
                    object status = controller.GetGameStatus(GameID, true);
                    string body = JsonConvert.SerializeObject(status);
                    Respond(body);
                }
                else if (payload == "false")
                {
                    object status = controller.GetGameStatus(GameID, false);
                    string body = JsonConvert.SerializeObject(status);
                    Respond(body);
                }
                else
                {
                    //if payload is none of these, we've messed up
                    Respond("error");
                }
            }
            //if controller call resulted in conflict or forbidden, respond with those
            catch (HttpResponseException e)
            {
                if (e.Code == System.Net.HttpStatusCode.Forbidden)
                {
                    Respond("Forbidden");
                }
                else
                {
                    Respond("Conflict");
                }
            }

        }

        /// <summary>
        /// Takes the given response body and asks the socket to send a formatted http response
        /// </summary>
        private void Respond(string ResponseBody)
        {
            string Response;

            if (ResponseBody == "Conflict")
            {
                Response = "HTTP/1.1 409 Conflict\r\n" +
                           "Content-Length: 0\r\n" +
                           "Content-Type: application/json; charset=utf-8\r\n" +
                           "\r\n";
            }
            else if (ResponseBody == "Forbidden")
            {
                Response = "HTTP/1.1 403 Forbidden\r\n" +
                           "Content-Length: 0\r\n" +
                           "Content-Type: application/json; charset=utf-8\r\n" +
                           "\r\n";
            }
            else if (ResponseBody == "NoContent")
            {
                Response = "HTTP/1.1 204 NoContent\r\n" +
                           "Content-Length: 0\r\n" +
                           "Content-Type: application/json; charset=utf-8\r\n" +
                           "\r\n";
            }
            else if (ResponseBody == "error")
            {
                Response = "HTTP/1.1 500 InternalServerError\r\n" +
                           "Content-Length: 0\r\n" +
                           "Content-Type: application/json; charset=utf-8\r\n" +
                           "\r\n";
            }
            else
            {
                //find the length of encoded body
                int ContentLength = Encoding.UTF8.GetByteCount(ResponseBody);
                Response = "HTTP/1.1 200 OK\r\n" +
                            "Content-Length: " + ContentLength + "\r\n" +
                            "Content-Type: application/json; charset=utf-8\r\n" +
                            "\r\n" +
                            ResponseBody;
            }

            //ask socket to send full response
            socket.BeginSend(Response, EndSend, null);

        }


        /// <summary>
        /// shutdown socket when begin send is finished
        /// </summary>
        private void EndSend(bool wasSent, object payload)
        {
            socket.Shutdown(SocketShutdown.Both);
        }
    }
}
