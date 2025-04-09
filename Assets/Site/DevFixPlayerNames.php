<?php
// Load configuration
$config = require 'config.php';

$servername = $config['servername'];
$username = $config['username'];
$password = $config['password'];
$dbname = $config['dbname'];

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

// List of GameLeaderboard tables
$leaderboardTables = [
    'Game22Leaderboards',
    'Game23Leaderboards',
    'Game24Leaderboards',
    'Game25Leaderboards',
    'Game26Leaderboards',
    'Game27Leaderboards'
];

// Iterate through each leaderboard table
foreach ($leaderboardTables as $table) {
    $query = "SELECT PlayerID, PlayerName FROM $table";
    $result = $conn->query($query);

    if ($result === false) {
        die("Query failed: " . $conn->error);
    }

    while ($row = $result->fetch_assoc()) {
        $playerID = $row['PlayerID'];
        $playerName = $row['PlayerName'];

        // Check if PlayerID exists in Players table
        $checkQuery = "SELECT COUNT(*) FROM Players WHERE PlayerID = ?";
        $stmt = $conn->prepare($checkQuery);
        if ($stmt === false) {
            die("Prepare failed: " . $conn->error);
        }
        $stmt->bind_param("i", $playerID);
        if ($stmt->execute() === false) {
            die("Execute failed: " . $stmt->error);
        }
        $stmt->bind_result($count);
        $stmt->fetch();
        $stmt->close();

        // If PlayerID does not exist, insert into Players table
        if ($count == 0) {
            $insertPlayerQuery = "INSERT INTO Players (PlayerID, PlayerName) VALUES (?, ?)";
            $stmt = $conn->prepare($insertPlayerQuery);
            if ($stmt === false) {
                die("Prepare failed: " . $conn->error);
            }
            $stmt->bind_param("is", $playerID, $playerName);
            if ($stmt->execute() === false) {
                die("Execute failed: " . $stmt->error);
            }
            $stmt->close();
        }

        // Insert into PlayerPreviousNames table, avoiding duplicates
        $insertQuery = "INSERT INTO PlayerPreviousNames (PlayerID, PreviousName) VALUES (?, ?)
                        ON DUPLICATE KEY UPDATE PreviousName = PreviousName";
        $stmt = $conn->prepare($insertQuery);
        if ($stmt === false) {
            die("Prepare failed: " . $conn->error);
        }
        $stmt->bind_param("is", $playerID, $playerName);
        if ($stmt->execute() === false) {
            die("Execute failed: " . $stmt->error);
        }
        $stmt->close();
    }

    $result->free();
}

// Close connection
$conn->close();

echo "Player names have been added to PlayerPreviousNames table successfully.";
?>
