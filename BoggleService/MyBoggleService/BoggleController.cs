using BoggleService.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace BoggleService.Controllers
{
    /// <summary>
    /// 
    /// This Boggle service keeps track of a set of users, a set of active and completed games, and 
    /// exactly one pending game. 
    /// 
    /// The API makes uses user tokens for authentication. User tokens are 
    /// strings that are unique and extremely difficult to guess. This implies that they must be long 
    /// and randomly generated. (A good way to generate such a token in C# is with Guid.NewGuid().)
    /// 
    /// The API also makes use of game IDs to identify games. Game IDs are strings that are unique, 
    /// short, and embeddable in the path portion of URLs. Strings made from small, consecutively
    /// generated integers are good for this purpose. 
    /// 
    /// A user token is valid if it is non-null and 
    /// identifies a user. A game token is valid if it is non-null and identifies a game. 
    /// The pending game has a game ID. If one player belongs to the pending game, it also has a user ID and a 
    /// requested time limit. Think of it as a game that is waiting for a second player to join. 
    /// There is always exactly one pending game. 
    /// 
    /// An active or completed game consists of: 
    /// A unique game token. A game state that is either "active" 
    /// (game currently being played) or "completed" (game for which time has expired). Any number of 
    /// games can be active and/or completed. 
    /// The user tokens of the first and second players to join the game. The 16-character string that 
    /// represents the contents of the game board. 
    /// The time limit of the game, in seconds. 
    /// The time remaining in the game, in seconds. 
    /// The current scores of the two players. Lists of word/score pairs for the two players. 
    /// A word/score pair consists of a word that was played and the score received for that word. 
    /// The words appear in the order in which they were played. 
    /// 
    /// The exact response to undefined or malformed HTTP requests is server-dependent and is not specified in 
    /// this API, except to note that some sort of error status code will be included in the response.
    /// 
    /// The response status code 500 (InternalServerError) is used to report, among other things, 
    /// unhandled exceptions in the service. Sometimes, trying the same service call again will 
    /// succeed, depending on the nature of the error.
    /// </summary>
    public class BoggleController
    {

        public class HttpResponseException : Exception
        {
            public HttpStatusCode Code { get; private set; }

            public HttpResponseException(HttpStatusCode c)
            {
                Code = c;
            }
        }



        /// <summary>
        /// Create a new user.
        /// If Nickname is null, or when trimmed is empty or longer than 50 characters, responds with status 403 (Forbidden).
        /// Otherwise, creates a new user with a unique user token and the trimmed Nickname.The returned user token should be used to identify the user in subsequent requests. Responds with status 200 (Ok).
        /// </summary>
        // [Route("BoggleService/users")]
        //[FromBody]
        public string PostRegister(string name)
        {
            if (name == "stall")
            {
                Thread.Sleep(5000);
            }
            lock (sync)
            {
                if (name == null || name.Trim().Length == 0)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                else
                {
                    Player p = new Player();
                    p.Nickname = name;
                    Players.Add(p.UserToken, p);
                    return p.UserToken;
                }
            }
        }

        /// <summary>
        /// Join a game. If UserToken is invalid, TimeLimit less than 5, or TimeLimit > 120, responds with status 403 (Forbidden). 
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict). Otherwise, 
        /// if there are no players in the pending game, adds UserToken as the first player of the pending game, 
        /// and the TimeLimit as the pending game's requested time limit. Returns an object as illustrated below containing
        /// the pending game's game ID. Responds with status 200 (Ok). Otherwise, adds UserToken as the second player. The pending 
        /// game becomes active and a new pending game with no players is created. The active game's time limit is the integer 
        /// average of the time limits requested by the two players. Returns an object as illustrated below containing the new active 
        /// game's game ID (which should be the same as the old pending game's game ID). Responds with status 200 (Ok).
        /// </summary>
        //[Route("BoggleService/games")]
        public JoinGameReturn PostJoinGame(JoinGameInput input)
        {

            lock (sync)
            {
                if (input.TimeLimit < 5 || input.TimeLimit > 120 || !Players.ContainsKey(input.UserToken))
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                else if (PendingGame != null)
                {
                    if (Games[PendingGame].Player1.UserToken == input.UserToken)
                    {
                        throw new HttpResponseException(HttpStatusCode.Conflict);
                    }
                    else
                    {
                        //pending game already exists
                        Games[PendingGame].Player2 = Players[input.UserToken];
                        //Start Game
                        Games[PendingGame].TimeLimit = (Games[PendingGame].TimeLimit + input.TimeLimit) / 2;
                        JoinGameReturn r = new JoinGameReturn();
                        r.GameID = PendingGame;
                        r.IsPending = false;

                        Games[PendingGame].StartGame();
                        PendingGame = null;
                        return r;
                    }
                }
                else
                {
                    //no one else is waiting - create pending game
                    Game newGame = new Game();
                    Games.Add(newGame.GameID, newGame);
                    PendingGame = newGame.GameID;
                    Games[PendingGame].Player1 = Players[input.UserToken];
                    Games[PendingGame].TimeLimit = input.TimeLimit;
                    JoinGameReturn r = new JoinGameReturn();
                    r.GameID = newGame.GameID;
                    r.IsPending = true;
                    return r;
                }
            }
        }

        /// <summary>
        /// Cancel a pending request to join a game. If UserToken is invalid or is not a player in the pending game, 
        /// responds with status 403 (Forbidden). Otherwise, removes UserToken from the pending game and responds with 
        /// status 204 (NoContent).
        /// </summary>
        //[Route("BoggleService/games")]
        //[FromBody]
        public void PutCancelGame(string user)
        {
            lock (sync)
            {
                if (!Players.ContainsKey(user) || PendingGame == null ||
                Games[PendingGame].Player1.UserToken != user)
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                else
                {

                    Games.Remove(PendingGame);
                    PendingGame = null;
                }
            }

        }

        /// <summary>
        /// Play a word in a game. If Word is null or empty or longer than 30 characters when trimmed, or if gameID or UserToken is invalid,
        /// or if UserToken is not a player in the game identified by gameID, responds with response code 403 (Forbidden). 
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict). 
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by gameID. 
        /// Returns the score for Word in the context of the game (e.g. if Word has been played before the score is zero). 
        /// Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        //[Route("BoggleService/games/{gameID}")]
        //[FromUri]
        public int PutPlayWord(string gameID, PlayWordInput input)
        {
            if (input.Word == null || input.Word == "" || input.Word.Trim().Length > 30)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
            else if (gameID == null || input.UserToken == null || !Games.ContainsKey(gameID))
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            //check if game state has changed
            Games[gameID].UpdateTime();

            if (!(Games[gameID].GameState == "active"))
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }
            else
            {
                int score = ScoreWord(gameID, input);
                if (Games[gameID].Player1.UserToken == input.UserToken)
                {
                    if (Games[gameID].Player1.WordsPlayed.ContainsKey(input.Word))
                    {
                        score = 0;
                    }
                    else
                    {
                        Games[gameID].Player1.WordsPlayed.Add(input.Word, score);
                        Games[gameID].Player1.Score += score;
                    }
                }
                else if (Games[gameID].Player2.UserToken == input.UserToken)
                {
                    if (Games[gameID].Player2.WordsPlayed.ContainsKey(input.Word))
                    {
                        score = 0;
                    }
                    else
                    {
                        Games[gameID].Player2.WordsPlayed.Add(input.Word, score);
                        Games[gameID].Player2.Score += score;
                    }
                }
                else
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                return score;
            }
        }

        /// <summary>
        /// Get game status information. If gameID is invalid, responds with status 403 (Forbidden). 
        /// Otherwise, returns information about the game named by gameID as illustrated below. 
        /// Note that the information returned depends on whether brief is true or false as well as on the state of the game. 
        /// Responds with status code 200 (OK). Note: The Board and Words are not case sensitive.
        /// </summary>
        //[Route("BoggleService/games/{gameID}/{brief}")]
        //[FromUri]
        //[FromUri]
        public Object GetGameStatus(string gameID, bool brief)
        {

            lock (sync)
            {
                //check if game id is valid
                if (!Games.ContainsKey(gameID))
                {
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }

                //check if game state has changed
                Games[gameID].UpdateTime();

                if (Games[gameID].GameState == "active")
                {
                    if (brief)
                    {
                        ActiceTrue returnInfo = new ActiceTrue();
                        returnInfo.GameState = "active";
                        returnInfo.TimeLeft = returnInfo.TimeLeft = Games[gameID].UpdateTime();
                        returnInfo.Player1 = new PlayerReturn();
                        returnInfo.Player1.Score = Games[gameID].Player1.Score;
                        returnInfo.Player2 = new PlayerReturn();
                        returnInfo.Player2.Score = Games[gameID].Player2.Score;
                        return returnInfo;
                    }
                    else
                    {
                        ActiveFalse returnInfo = new ActiveFalse();
                        Games[gameID].UpdateTime();
                        returnInfo.Board = Games[gameID].Board;
                        returnInfo.GameState = "active";
                        returnInfo.TimeLeft = Games[gameID].UpdateTime();
                        returnInfo.TimeLimit = Games[gameID].TimeLimit;
                        returnInfo.Player1 = new PlayerReturn2();
                        returnInfo.Player1.Score = Games[gameID].Player1.Score;
                        returnInfo.Player2 = new PlayerReturn2();
                        returnInfo.Player2.Score = Games[gameID].Player2.Score;
                        returnInfo.Player1.Nickname = Games[gameID].Player1.Nickname;
                        returnInfo.Player2.Nickname = Games[gameID].Player2.Nickname;
                        return returnInfo;

                    }

                }
                else if (Games[gameID].GameState == "pending")
                {
                    PendingTrue returnInfo = new PendingTrue();
                    returnInfo.GameState = "pending";
                    return returnInfo;

                }
                else //Game state is complete
                {
                    if (brief)
                    {
                        CompleteTrue returnInfo = new CompleteTrue();
                        returnInfo.GameState = "completed";
                        returnInfo.Player1 = new PlayerReturn();
                        returnInfo.Player1.Score = Games[gameID].Player1.Score;
                        returnInfo.Player2 = new PlayerReturn();
                        returnInfo.Player2.Score = Games[gameID].Player2.Score;
                        return returnInfo;


                    }
                    else
                    {
                        CompleteFalse returnInfo = new CompleteFalse();
                        returnInfo.GameState = "completed";
                        returnInfo.Board = Games[gameID].Board;
                        returnInfo.TimeLimit = Games[gameID].TimeLimit;
                        returnInfo.Player1 = new PlayerReturn3();
                        returnInfo.Player1.WordsPlayed = new List<Word1>();
                        returnInfo.Player1.Score = Games[gameID].Player1.Score;
                        returnInfo.Player1.Nickname = Games[gameID].Player1.Nickname;
                        foreach (string word in Games[gameID].Player1.WordsPlayed.Keys)
                        {
                            Word1 addWord = new Word1();
                            addWord.Word = word;
                            addWord.Score = Games[gameID].Player1.WordsPlayed[word];
                            returnInfo.Player1.WordsPlayed.Add(addWord);
                        }
                        returnInfo.Player2 = new PlayerReturn3();
                        returnInfo.Player2.WordsPlayed = new List<Word1>();
                        returnInfo.Player2.Score = Games[gameID].Player2.Score;
                        returnInfo.Player2.Nickname = Games[gameID].Player2.Nickname;
                        foreach (string word in Games[gameID].Player2.WordsPlayed.Keys)
                        {
                            Word1 addWord = new Word1();
                            addWord.Word = word;
                            addWord.Score = Games[gameID].Player2.WordsPlayed[word];
                            returnInfo.Player2.WordsPlayed.Add(addWord);
                        }

                        return returnInfo;

                    }

                }

            }
        }

        /// <summary>
        /// scores a word
        /// </summary>
        private int ScoreWord(string gameID, PlayWordInput input)
        {
            bool isValid = Games[gameID].BBoard.CanBeFormed(input.Word) && IsInDictionary(input.Word);
            string word = input.Word;
            string token = input.UserToken;
            if (word.Length < 3)
            {
                return 0;
            }
            else if (Players[token].WordsPlayed.ContainsKey(word))
            {
                return 0;
            }
            else if (word.Length < 5 && isValid)
            {
                return 1;
            }
            else if (word.Length == 5 && isValid)
            {
                return 2;
            }
            else if (word.Length == 6 && isValid)
            {
                return 3;
            }
            else if (word.Length == 7 && isValid)
            {
                return 5;
            }
            else if (word.Length > 7 && isValid)
            {
                return 11;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns true if word is in dictionary
        /// </summary>
        private bool IsInDictionary(string word)
        {
            using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
            {
                string dictionary = sr.ReadToEnd();
                if (dictionary.Contains("\r\n" + word.ToUpper() + "\r\n"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// sync object for locks
        /// </summary>
        private static readonly object sync = new object();

        /// <summary>
        /// Database of all players who have registered. Identified by their unique User Token.
        /// </summary>
        private static Dictionary<string, Player> Players = new Dictionary<String, Player>();

        /// <summary>
        /// The game ID of the pending game (if there is one, null otherwise)
        /// </summary>
        private static String PendingGame;

        /// <summary>
        /// Database of all games that have been requested, identified by unique game ID
        /// </summary>
        private static Dictionary<String, Game> Games = new Dictionary<String, Game>();
    }

    /// <summary>
    /// Represents a boggle game with a board and players
    /// </summary>
    public class Game
    {
        /// <summary>
        /// active, pending, or completed
        /// </summary>
        public string GameState;

        /// <summary>
        /// Letters on the boggle board
        /// </summary>
        public string Board;

        /// <summary>
        /// Unique identifier for this game
        /// </summary>
        public string GameID;

        /// <summary>
        /// First player in game
        /// </summary>
        public Player Player1;

        /// <summary>
        /// second player in game
        /// </summary>
        public Player Player2;

        /// <summary>
        /// duration of game in seconds
        /// </summary>
        public int TimeLimit;

        /// <summary>
        /// true if game state is pending
        /// </summary>
        public bool IsPending;

        /// <summary>
        /// boggle board object for this game
        /// </summary>
        public Boggle.BoggleBoard BBoard;

        /// <summary>
        /// the time the game was started
        /// </summary>
        public DateTime StartTime;

        /// <summary>
        /// creates a pending game with null players and a board
        /// </summary>
        public Game()
        {
            GameState = "pending";
            IsPending = true;
            BBoard = new Boggle.BoggleBoard();
            Board = BBoard.ToString();
            GameID = Guid.NewGuid().ToString();
            StartTime = new DateTime();
        }

        /// <summary>
        /// returns the time remaining in the game and sets game state to completed if that time is 0
        /// </summary>
        /// <returns></returns>
        public int UpdateTime()
        {
            if (GameState == "active")
            {
                // time left = time limit - (current time - start time)
                int timeLeft = TimeLimit - (int)(DateTime.Now.Subtract(StartTime)).TotalSeconds;
                if (timeLeft > 0)
                {
                    return timeLeft;
                }
                else
                {
                    GameState = "completed";
                    return 0;
                }
            }
            return -1;

        }

        /// <summary>
        /// Begins the game, sets state to active, restes player fields
        /// </summary>
        public void StartGame()
        {
            GameState = "active";
            IsPending = false;
            Player1.Score = 0;
            Player1.WordsPlayed = new Dictionary<string, int>();
            Player2.Score = 0;
            Player2.WordsPlayed = new Dictionary<string, int>();
            StartTime = DateTime.Now;
        }


    }

    /// <summary>
    /// Represnets a boggle player
    /// </summary>
    public class Player
    {
        /// <summary>
        /// the words the player has submitted in most recent game
        /// </summary>
        public Dictionary<string, int> WordsPlayed;

        /// <summary>
        /// the final score for the most recent game
        /// </summary>
        public int Score;

        /// <summary>
        /// unique identifier for player
        /// </summary>
        public string UserToken;

        /// <summary>
        /// player's chosen name
        /// </summary>
        public string Nickname;

        public Player()
        {
            UserToken = Guid.NewGuid().ToString();
            WordsPlayed = new Dictionary<string, int>();
        }
    }
}


