# BoggleService

BoggleService is a server application written in C# that provides a Boggle game service. It allows clients to register, join games, cancel games, play words, and get game status through a RESTful API. The server works in conjunction with the BoggleClient application on my Github page.

## API Endpoints

The MyBoggleService server exposes the following API endpoints:

- `POST /BoggleService/users`: Register a new user.
- `POST /BoggleService/games`: Join a new game.
- `PUT /BoggleService/games`: Cancel an ongoing game.
- `PUT /BoggleService/games/{gameId}`: Play a word in the specified game.
- `GET /BoggleService/games/{gameId}`: Get the status of the specified game.

## Request and Response Formats

The server expects and returns data in JSON format. Below is an overview of the request and response formats for each endpoint:

### Register a User (`POST /BoggleService/users`)

**Request:**

```json
{
  "NickName": "JohnDoe"
}
```

**Response:**

```json
{
  "UserToken": "abcd1234"
}
```

### Join a Game (`POST /BoggleService/games`)

**Request:**

```json
{
  "UserToken": "abcd1234"
}
```

**Response:**

```json
{
  "GameID": "game123",
  "Board": "CDEFNRTYIODEUIVX",
  "TimeLimit": 180
}
```

### Cancel a Game (`PUT /BoggleService/games`)

**Request:**

```json
{
  "UserToken": "abcd1234"
}
```

**Response:**

No content (HTTP status code 204)

### Play a Word (`PUT /BoggleService/games/{gameId}`)

**Request:**

```json
{
  "UserToken": "abcd1234",
  "Word": "cat"
}
```

**Response:**

```json
{
  "Score": 10
}
```

### Get Game Status (`GET /BoggleService/games/{gameId}`)

**Request:**

No request body

**Response:**

```json
{
  "Status": "InProgress",
  "TimeRemaining": 120
}
```

##Contact Information

For any questions or feedback regarding BoogleService, please feel free to contact Saleema Qazi at saleemasqazi@gmail.com

I hope you enjoy!
