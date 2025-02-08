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

// Function to display player list
function displayPlayerList($conn) {
    $sql = "SELECT PlayerID, PlayerName, GamesPlayed, TotalSoldiers, TotalWorkers FROM Players";
    $result = $conn->query($sql);

    $players = [];
    while ($row = $result->fetch_assoc()) {
        $players[] = $row;
    }

    echo "<h1>Player List</h1>";
    echo "<div>";
    echo "<label for='minGames'>Minimum Games:</label>";
    echo "<input type='range' id='minGames' name='minGames' min='1' max='6' value='1' oninput='updateMinGamesLabel(this.value)' onchange='filterPlayers()'>";
    echo "<span id='minGamesLabel'>1</span>";
    echo "</div>";
    echo "<table id='playerTable'>";
    echo "<thead>";
    echo "<tr>";
    echo "<th onclick='sortTable(0, \"num\")'>Player ID</th>";
    echo "<th onclick='sortTable(1, \"alpha\")'>Player Name</th>";
    echo "<th onclick='sortTable(2, \"num\", true)'>Games Played</th>";
    echo "<th onclick='sortTable(3, \"num\", true)'>Soldiers Sent</th>";
    echo "<th onclick='sortTable(4, \"num\", true)'>Workers Sent</th>";
    echo "<th onclick='sortTable(5, \"avgSoldiers\", true)'>Avg Soldiers/Game</th>";
    echo "<th onclick='sortTable(6, \"avgWorkers\", true)'>Avg Workers/Game</th>";
    echo "</tr>";
    echo "</thead>";
    echo "<tbody id='playerTableBody'>";
    echo "</tbody>";
    echo "</table>";

    echo "<div>";
    echo "<label for='playersPerPage'>Players per page:</label>";
    echo "<select id='playersPerPage' onchange='changePlayersPerPage()'>";
    echo "<option value='25'>25</option>";
    echo "<option value='50'>50</option>";
    echo "<option value='100' selected>100</option>";
    echo "<option value='250'>250</option>";
    echo "<option value='500'>500</option>";
    echo "</select>";
    echo "</div>";

    echo "<div id='paginationControls'></div>";

    echo "<script>";
    echo "var players = " . json_encode($players) . ";";
    echo "var currentPage = 1;";
    echo "var playersPerPage = localStorage.getItem('playersPerPage') || 100;";
    echo "var sortColumn = 0;";
    echo "var sortOrder = 'asc';";
    echo "var minGames = localStorage.getItem('minGames') || 1;";
    echo "document.addEventListener('DOMContentLoaded', function() {";
    echo "    document.getElementById('minGames').value = minGames;";
    echo "    document.getElementById('minGamesLabel').innerText = minGames;";
    echo "    document.getElementById('playersPerPage').value = playersPerPage;";
    echo "    sortTable(sortColumn, 'num');"; // Initialize sorting
    echo "});";
    echo "</script>";
}

displayPlayerList($conn);

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
            aValue = a[Object.keys(a)[columnIndex]];
            bValue = b[Object.keys(b)[columnIndex]];
        } else if (type === "alpha") {
            aValue = a[Object.keys(a)[columnIndex]].toLowerCase();
            bValue = b[Object.keys(b)[columnIndex]].toLowerCase();
        } else if (type === "avgSoldiers") {
            aValue = a.GamesPlayed > 0 ? a.TotalSoldiers / a.GamesPlayed : 0;
            bValue = b.GamesPlayed > 0 ? b.TotalSoldiers / b.GamesPlayed : 0;
        } else if (type === "avgWorkers") {
            aValue = a.GamesPlayed > 0 ? a.TotalWorkers / a.GamesPlayed : 0;
            bValue = b.GamesPlayed > 0 ? b.TotalWorkers / b.GamesPlayed : 0;
        }

        if (descending) {
            return bValue - aValue;
        } else {
            return aValue - bValue;
        }
    });
    sortColumn = columnIndex;
    sortOrder = descending ? 'desc' : 'asc';
    displayPlayers();
}

function displayPlayers() {
    var tableBody = document.getElementById("playerTableBody");
    tableBody.innerHTML = "";
    var start = (currentPage - 1) * playersPerPage;
    var end = start + playersPerPage;
    for (var i = start; i < end && i < players.length; i++) {
        if (players[i].GamesPlayed >= minGames) {
            var avgSoldiers = players[i].GamesPlayed > 0 ? players[i].TotalSoldiers / players[i].GamesPlayed : 0;
            var avgWorkers = players[i].GamesPlayed > 0 ? players[i].TotalWorkers / players[i].GamesPlayed : 0;
            var row = "<tr onclick='viewPlayerProfile(" + players[i].PlayerID + ")'>";
            row += "<td>" + players[i].PlayerID + "</td>";
            row += "<td>" + players[i].PlayerName + "</td>";
            row += "<td>" + players[i].GamesPlayed + "</td>";
            row += "<td>" + players[i].TotalSoldiers + "</td>";
            row += "<td>" + players[i].TotalWorkers + "</td>";
            row += "<td>" + avgSoldiers.toFixed(2) + "</td>";
            row += "<td>" + avgWorkers.toFixed(2) + "</td>";
            row += "</tr>";
            tableBody.innerHTML += row;
        }
    }
    updatePaginationControls();
}

function updateMinGamesLabel(value) {
    document.getElementById('minGamesLabel').innerText = value;
    minGames = parseInt(value);
    localStorage.setItem('minGames', minGames);
    filterPlayers();
}

function filterPlayers() {
    displayPlayers();
}

function changePlayersPerPage() {
    playersPerPage = parseInt(document.getElementById('playersPerPage').value);
    localStorage.setItem('playersPerPage', playersPerPage);
    displayPlayers();
}

function updatePaginationControls() {
    var paginationControls = document.getElementById('paginationControls');
    paginationControls.innerHTML = "";
    var totalPages = Math.ceil(players.length / playersPerPage);
    for (var i = 1; i <= totalPages; i++) {
        var pageButton = "<button onclick='goToPage(" + i + ")'>" + i + "</button>";
        paginationControls.innerHTML += pageButton;
    }
}

function goToPage(page) {
    currentPage = page;
    displayPlayers();
}
</script>

<style>
    body {
        font-family: Arial, sans-serif;
    }
    table {
        width: 100%;
        border-collapse: collapse;
    }
    table, th, td {
        border: 1px solid black;
    }
    th, td {
        padding: 8px;
        text-align: left;
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
    }
</style>
