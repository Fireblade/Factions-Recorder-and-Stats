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

$gameID = isset($_GET['GameID']) ? intval($_GET['GameID']) : null;
$condenseTop = isset($_GET['condenseTop']);

if ($gameID === null) {
    // Display list of games
    $sql = "SELECT GameID FROM ActiveGames WHERE GameID >= 20";
    $result = $conn->query($sql);

    echo "<h1>Game List</h1>";
    echo "<div class='game-list'>";
    while ($row = $result->fetch_assoc()) {
        echo "<button class='game-button' onclick='window.location.href=\"?GameID=" . $row['GameID'] . "\"'>Game " . $row['GameID'] . " Leaderboards</button>";
    }
    echo "</div>";
} else {
    // Check if the game table exists
    $tableName = "Game" . $gameID . "Leaderboards";
    $sql = "SHOW TABLES LIKE '$tableName'";
    $result = $conn->query($sql);

    if ($result->num_rows == 0) {
        // Table does not exist, show list of games
        echo "<h1>Game $gameID Leaderboard does not exist</h1>";
        echo "<a href='?'>Back to Game List</a>";
    } else {
        // Display game leaderboard
        if (!$condenseTop) {
            echo "<div style='display: flex; align-items: center;'>";
            echo "<button onclick='viewPlayerList()'>Back to Game List</button>";
            if ($gameID == 22) {
                echo "<h1 style='margin-left: 10px;'>Game 22 Leaderboards (Temporarily missing data)</h1>";
            } else {
                echo "<h1 style='margin-left: 10px;'>Game $gameID Leaderboard</h1>";
            }
            echo "</div>";
        }

        // Add team filter buttons
        echo "<div class='team-filters' style='display: flex; align-items: center;'>";
        echo "<button class='team-button' id='redTeam' onclick='toggleTeam(\"RED\")' style='background-color: #4d0000;'>RED</button>";
        echo "<button class='team-button' id='greenTeam' onclick='toggleTeam(\"GREEN\")' style='background-color: #004d00;'>GREEN</button>";
        echo "<button class='team-button' id='blueTeam' onclick='toggleTeam(\"BLUE\")' style='background-color: #00004d;'>BLUE</button>";
        echo "<button class='team-button' id='yellowTeam' onclick='toggleTeam(\"YELLOW\")' style='background-color: #4d4d00;'>YELLOW</button>";
        if ($condenseTop) {
            if ($gameID == 22) {
                echo "<h1 style='margin-left: 10px;'>Game 22 Leaderboards (Temporarily missing data)</h1>";
            } else {
                echo "<h1 style='margin-left: 10px;'>Game $gameID Leaderboard</h1>";
            }
        }
        echo "</div>";

        $sql = "SELECT PlayerID, PlayerName, Team, HQLevel, Soldiers, Workers, TilesCaptured, FPRating FROM $tableName";
        $result = $conn->query($sql);

        $players = [];
        $multiplier = ($gameID >= 23) ? 1.8 : 2;
        while ($row = $result->fetch_assoc()) {
            $row['MVPScore'] = round(($row['Soldiers'] * $multiplier) + $row['Workers']);
            $players[] = $row;
        }

        echo "<table id='playerTable'>";
        echo "<thead>";
        echo "<tr>";
        echo "<th onclick='sortTable(0, \"num\")'>Player ID</th>";
        echo "<th onclick='sortTable(1, \"alpha\")'>Player Name</th>";
        echo "<th onclick='sortTable(2, \"alpha\")'>Team</th>";
        echo "<th onclick='sortTable(3, \"num\", true)'>HQ Level</th>";
        echo "<th onclick='sortTable(4, \"num\", true)'>Soldiers</th>";
        echo "<th onclick='sortTable(5, \"num\", true)'>Workers</th>";
        echo "<th onclick='sortTable(8, \"num\", true)'>MVP Score</th>";
        echo "<th onclick='sortTable(6, \"num\", true)'>Tiles Captured</th>";
        echo "<th onclick='sortTable(7, \"num\", true)'>FP Rating</th>";
        echo "</tr>";
        echo "</thead>";
        echo "<tbody id='playerTableBody'>";
        echo "</tbody>";
        echo "</table>";

        echo "<script>";
        echo "var players = " . json_encode($players) . ";";
        echo "var currentPage = 1;";
        echo "var playersPerPage = 200;";
        echo "var sortColumn = 8;"; // Default sort by MVP Score
        echo "var sortOrder = 'desc';"; // Default sort order
        echo "var teamFilters = { RED: true, GREEN: true, BLUE: true, YELLOW: true };";
        echo "var gameID = " . $gameID . ";"; // Pass gameID to JavaScript
        echo "document.addEventListener('DOMContentLoaded', function() {";
        echo "    sortTable(sortColumn, 'num', true);"; // Initialize sorting
        echo "    window.addEventListener('scroll', onScroll);"; // Add scroll event listener
        echo "});";
        echo "</script>";
    }
}

// Close connection
$conn->close();
?>

<script>
function viewPlayerProfile(playerID) {
    window.location.href = "Player.php?userID=" + playerID;
}

function viewPlayerList() {
    window.location.href = "?";
}

function sortTable(columnIndex, type, descending = false) {
    players.sort(function(a, b) {
        let aValue, bValue;
        if (type === "num") {
            aValue = Number(a[Object.keys(a)[columnIndex]]);
            bValue = Number(b[Object.keys(b)[columnIndex]]);
        } else if (type === "alpha") {
            aValue = a[Object.keys(a)[columnIndex]].toLowerCase();
            bValue = b[Object.keys(b)[columnIndex]].toLowerCase();
        }

        if (descending) {
            return bValue - aValue;
        } else {
            return aValue - bValue;
        }
    });
    sortColumn = columnIndex;
    sortOrder = descending ? 'desc' : 'asc';
    currentPage = 1; // Reset to the first page
    displayPlayers(true); // Clear existing rows
}

function displayPlayers(clear = false) {
    var tableBody = document.getElementById("playerTableBody");
    if (clear) {
        tableBody.innerHTML = ""; // Clear existing rows
    }
    var start = (currentPage - 1) * playersPerPage;
    var end = start + playersPerPage;
    if (start >= players.length) return; // No more players to display
    for (var i = start; i < end && i < players.length; i++) {
        if (!players[i].PlayerName || !teamFilters[players[i].Team]) {
            continue;
        }
        var teamName = players[i].Team;
        var teamColor = getTeamColor(players[i].Team);
        if (gameID == 20) {
            teamName = "??";
            teamColor = "white";
        }
        var row = "<tr onclick='viewPlayerProfile(" + players[i].PlayerID + ")' style='color: " + teamColor + "; font-weight: bold;'>";
        row += "<td>" + (players[i].PlayerID == 0 ? '?' : players[i].PlayerID) + "</td>";
        row += "<td>" + players[i].PlayerName + "</td>";
        row += "<td>" + teamName + "</td>";
        row += "<td>" + Number(players[i].HQLevel).toLocaleString() + "</td>";
        row += "<td>" + Number(players[i].Soldiers).toLocaleString() + "</td>";
        row += "<td>" + Number(players[i].Workers).toLocaleString() + "</td>";
        row += "<td>" + Number(players[i].MVPScore).toLocaleString() + "</td>";
        row += "<td>" + Number(players[i].TilesCaptured).toLocaleString() + "</td>";
        row += "<td>" + Number(players[i].FPRating).toLocaleString() + "</td>";
        tableBody.innerHTML += row;
    }
    currentPage++;
}

function onScroll() {
    if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight) {
        displayPlayers();
    }
}

function toggleTeam(team) {
    teamFilters[team] = !teamFilters[team];
    document.getElementById(team.toLowerCase() + 'Team').style.opacity = teamFilters[team] ? '1' : '0.5';
    currentPage = 1; // Reset to the first page
    displayPlayers(true); // Clear existing rows
}

function getTeamColor(team) {
    switch (team) {
        case 'RED':
            return '#FF9999';
        case 'GREEN':
            return '#99FF99';
        case 'BLUE':
            return '#9999FF';
        case 'YELLOW':
            return '#FFFF99';
        default:
            return 'white';
    }
}
</script>

<style>
    body {
        font-family: Arial, sans-serif;
        background-color: #8E6B43;
        color: white;
    }
    table {
        width: 100%;
        border-collapse: collapse;
    }
    table, th, td {
        border: 1px solid white;
    }
    th, td {
        padding: 8px;
        text-align: left;
        font-weight: bold;
    }
    th {
        cursor: pointer;
    }
    tr:hover {
        background-color: #f5f5f5;
        cursor: pointer;
    }
    button {
        margin-bottom: 20px;
        padding: 10px 20px;
        font-size: 16px;
        background-color: #333;
        color: white;
        border: none;
        cursor: pointer;
    }
    button:hover {
        background-color: #555;
    }
    .game-list {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
    }
    .game-button {
        background-color: black;
        color: white;
        border: none;
        padding: 10px 20px;
        cursor: pointer;
    }
    .game-button:hover {
        background-color: #555;
    }
    .team-filters {
        margin: 0px 0;
        display: flex;
        align-items: center;
    }
    .team-button {
        margin-right: 10px;
        padding: 10px 20px;
        color: white;
        font-weight: bold; /* Bolden the text */
        border: none;
        cursor: pointer;
    }
</style>
