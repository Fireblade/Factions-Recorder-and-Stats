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
    echo "var currentPage = localStorage.getItem('currentPage') ? parseInt(localStorage.getItem('currentPage')) : 1;";
    echo "var playersPerPage = localStorage.getItem('playersPerPage') ? parseInt(localStorage.getItem('playersPerPage')) : 100;";
    echo "var sortColumn = localStorage.getItem('sortColumn') ? parseInt(localStorage.getItem('sortColumn')) : 0;";
    echo "var sortOrder = localStorage.getItem('sortOrder') ? localStorage.getItem('sortOrder') : 'asc';";
    echo "var minGames = 1;";
    echo "document.getElementById('minGames').value = minGames;";
    echo "document.getElementById('minGamesLabel').innerText = minGames;";
    echo "filterPlayers();"; // Call filterPlayers to display the list immediately
    echo "</script>";
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
        echo "<h1>$playerName</h1>";
        echo "<p>Games Played: $gamesPlayed, Awards: $awards</p>";
        echo "<p>Red: $playedRed, Blue: $playedBlue, Green: $playedGreen, Yellow: $playedYellow</p>";
        echo "<br>";
        echo "<p>Total Soldiers: $totalSoldiers, Total Workers: $totalWorkers, Total Cases Captured: $totalCasesCaptured</p>";
        echo "<p>Average HQ Level per Game: $avgHQLevel</p>";
        echo "<p>Average Soldiers per Game: $avgSoldiers</p>";
        echo "<p>Average Workers per Game: $avgWorkers</p>";
        echo "<p>Average Cases Captured per Game: $avgCasesCaptured</p>";
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
    displayPlayerList($conn);
}

// Close connection
$conn->close();
?>

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

<script>
    function viewPlayerProfile(playerID) {
        saveState();
        window.location.href = "?userID=" + playerID;
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
        displayPaginationControls();
    }

    function changePlayersPerPage() {
        playersPerPage = parseInt(document.getElementById("playersPerPage").value);
        currentPage = 1;
        displayPlayers();
    }

    function displayPaginationControls() {
        var paginationControls = document.getElementById("paginationControls");
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

    function saveState() {
        localStorage.setItem('currentPage', currentPage);
        localStorage.setItem('playersPerPage', playersPerPage);
        localStorage.setItem('sortColumn', sortColumn);
        localStorage.setItem('sortOrder', sortOrder);
    }

    function updateMinGamesLabel(value) {
        document.getElementById('minGamesLabel').innerText = value;
    }

    function filterPlayers() {
        minGames = parseInt(document.getElementById('minGames').value);
        displayPlayers();
    }
</script>

