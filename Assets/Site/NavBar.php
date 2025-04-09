    <?php
    function renderNavbar($hideMenu = false, $extraContent = '') {
        if ($hideMenu) {
            return;
        }
        ?>
        <div id="topBanner">
            <button id="menuButton" onclick="toggleMenu()">
                <img src="https://www.mclama.com/Factions/Images/menu_list.png" alt="Menu">
            </button>
            <?php if ($extraContent): ?>
                <div id="extraContent">
                    <?php echo $extraContent; ?>
                </div>
            <?php endif; ?>
        </div>

        <div id="menu">
            <a href="https://mclama.com/Factions/TimeLapse.php">Timelapse</a>
            <a href="https://mclama.com/Factions/PlayerList.php">Player List</a>
            <a href="https://mclama.com/Factions/ProjectCostPlanner.php">Project Planner</a>
            <a href="https://mclama.com/Factions/GameLeaderboard.php">Game Leaderboard</a>
            <div class="section-label">Developer Stuff</div>
            <a href="https://mclama.com/Factions/Buildings/">Building Data</a>
            <a href="https://mclama.com/Factions/Maps/">Maps</a>
            <a href="https://mclama.com/Factions/Sounds/">Sounds</a>
            <a href="https://mclama.com/Factions/GameData/">Game Data</a>
        </div>
        <?php
    }
    ?>