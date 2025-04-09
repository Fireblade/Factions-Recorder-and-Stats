<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Load configuration
$config = require 'config.php';

$servername = $config['servername'];
$username = $config['username'];
$password = $config['password'];
$dbname = $config['dbname'];
$newGamePassword = $config['newgame_password'];

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $inputPassword = $_POST['newGamePassword'];
    if ($inputPassword !== $newGamePassword) {
        die("Invalid password.");
    }

    $gameID = intval($_POST['GameID']);
    $map = $_POST['Map'];
    $mapWidth = intval($_POST['MapWidth']);
    $mapHeight = intval($_POST['MapHeight']);
    $victoryGoal = intval($_POST['VictoryGoal']);
    $minuteSplit = intval($_POST['MinuteSplit']);
    $gameMode = $_POST['GameMode'];
    $buildingCostsVersion = intval($_POST['BuildingCostsVersion']);
    $expectedGameStart = $_POST['ExpectedGameStart'];
    $waitingToStart = true;

    $sql = "INSERT INTO ActiveGames (GameID, Map, MapWidth, MapHeight, VictoryGoal, MinuteSplit, GameMode, BuildingCostsVersion, ExpectedGameStart, waitingToStart)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("isiiissisb", $gameID, $map, $mapWidth, $mapHeight, $victoryGoal, $minuteSplit, $gameMode, $buildingCostsVersion, $expectedGameStart, $waitingToStart);

    if ($stmt->execute()) {
        echo "New game created successfully.";
    } else {
        echo "Error: " . $stmt->error;
    }

    $stmt->close();
}

$conn->close();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Create New Game</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #8E6B43;
            color: white;
        }
        form {
            display: flex;
            flex-direction: column;
            gap: 10px;
            max-width: 400px;
            margin: auto;
        }
        label {
            font-weight: bold;
        }
        input, select, button {
            padding: 10px;
            font-size: 16px;
            border: none;
            border-radius: 5px;
        }
        button {
            background-color: #333;
            color: white;
            cursor: pointer;
        }
        button:hover {
            background-color: #555;
        }
    </style>
</head>
<body>
    <h1>Create New Game</h1>
    <form method="POST">
        <label for="newGamePassword">New Game Password</label>
        <input type="password" id="newGamePassword" name="newGamePassword" required>

        <label for="GameID">Game ID</label>
        <input type="number" id="GameID" name="GameID" required>

        <label for="Map">Map</label>
        <input type="text" id="Map" name="Map" required>

        <label for="MapWidth">Map Width</label>
        <input type="number" id="MapWidth" name="MapWidth" value="50" required>

        <label for="MapHeight">Map Height</label>
        <input type="number" id="MapHeight" name="MapHeight" value="50" required>

        <label for="VictoryGoal">Victory Goal</label>
        <input type="number" id="VictoryGoal" name="VictoryGoal" value="100000" required>

        <label for="MinuteSplit">Minute Split</label>
        <input type="number" id="MinuteSplit" name="MinuteSplit" value="30" required>

        <label for="GameMode">Game Mode</label>
        <select id="GameMode" name="GameMode" required>
            <option value="Flash">Flash</option>
            <option value="Short">Short</option>
            <option value="Standard" selected>Standard</option>
        </select>

        <label for="BuildingCostsVersion">Building Costs Version</label>
        <input type="number" id="BuildingCostsVersion" name="BuildingCostsVersion" value="3" required>

        <label for="ExpectedGameStart">Expected Game Start</label>
        <input type="datetime-local" id="ExpectedGameStart" name="ExpectedGameStart" required>

        <button type="submit">Create</button>
    </form>
</body>
</html>
