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

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

// Function to display player profile
function displayPlayerProfile($conn, $userID) {
    $sql = "SELECT PlayerName, GamesPlayed, Awards, TotalCasesCaptured, TotalHQLevel, TotalSoldiers, TotalWorkers, PlayedRed, PlayedGreen, PlayedBlue, PlayedYellow FROM Players WHERE PlayerID = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $userID);
    $stmt->execute();
    $result = $stmt->get_result();

    if ($result->num_rows > 0) {
        // Fetch the player data
        $row = $result->fetch_assoc();
        $playerName = $row['PlayerName'];
        $gamesPlayed = $row['GamesPlayed'];
        $awards = $row['Awards'];
        $totalCasesCaptured = $row['TotalCasesCaptured'];
        $totalHQLevel = $row['TotalHQLevel'];
        $totalSoldiers = $row['TotalSoldiers'];
        $totalWorkers = $row['TotalWorkers'];
        $playedRed = $row['PlayedRed'];
        $playedGreen = $row['PlayedGreen'];
        $playedBlue = $row['PlayedBlue'];
        $playedYellow = $row['PlayedYellow'];

        // Calculate averages
        $avgHQLevel = $gamesPlayed > 0 ? $totalHQLevel / $gamesPlayed : 0;
        $avgSoldiers = $gamesPlayed > 0 ? $totalSoldiers / $gamesPlayed : 0;
        $avgWorkers = $gamesPlayed > 0 ? $totalWorkers / $gamesPlayed : 0;
        $avgCasesCaptured = $gamesPlayed > 0 ? $totalCasesCaptured / $gamesPlayed : 0;

        // Display player information
        echo "<div style='background-color: #8D6A3F; padding: 20px;'>";
        echo "<div style='border: 1px solid black; padding: 10px; margin-bottom: 10px;'>";
        echo "<h1>$playerName</h1>";
        echo "<p>Games Played: $gamesPlayed, Awards: $awards</p>";
        echo "<p>Red: $playedRed, Blue: $playedBlue, Green: $playedGreen, Yellow: $playedYellow</p>";
        echo "</div>";
        echo "<div style='border: 1px solid black; padding: 10px;'>";
        echo "<p>Total Soldiers: $totalSoldiers, Total Workers: $totalWorkers, Total Cases Captured: $totalCasesCaptured</p>";
        echo "<p>Average HQ Level per Game: $avgHQLevel</p>";
        echo "<p>Average Soldiers per Game: $avgSoldiers</p>";
        echo "<p>Average Workers per Game: $avgWorkers</p>";
        echo "<p>Average Cases Captured per Game: $avgCasesCaptured</p>";
        echo "</div>";
        echo "</div>";
    } else {
        echo "No player found with the given ID.";
    }
}

// Check if userID is set
if (isset($_GET['userID'])) {
    $userID = intval($_GET['userID']);
    echo "<button onclick='viewPlayerList()'>Player List</button>";
    displayPlayerProfile($conn, $userID);
} else {
    header("Location: PlayerList.php");
    exit;
}

// Close connection
$conn->close();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Player Profile</title>
    <style>
        body {
            background-color: #8D6A3F;
            color: #000;
            font-family: Arial, sans-serif;
        }
        .content {
            padding: 20px;
        }
        .box {
            border: 1px solid black;
            padding: 10px;
            margin-bottom: 10px;
        }
    </style>
</head>
<body>
    <script>
        function viewPlayerList() {
            window.location.href = "PlayerList.php";
        }
    </script>
</body>
</html>
