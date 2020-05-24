-- --------------------------------------------------------
-- Host:                         ilddor.com
-- Server version:               10.4.7-MariaDB-1:10.4.7+maria~buster - mariadb.org binary distribution
-- Server OS:                    debian-linux-gnu
-- HeidiSQL Version:             11.0.0.5919
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

-- Dumping structure for table Boderator.Missions
CREATE TABLE IF NOT EXISTS `Missions` (
  `SignupChannel` bigint(20) unsigned NOT NULL,
  `Title` varchar(100) DEFAULT NULL,
  `Date` datetime DEFAULT NULL,
  `CloseDate` datetime DEFAULT NULL,
  `Description` varchar(2000) DEFAULT NULL,
  `Attachment` varchar(1000) DEFAULT NULL,
  `Filename` varchar(100) DEFAULT NULL,
  `Modlist` varchar(200) DEFAULT NULL,
  `Owner` bigint(20) unsigned DEFAULT NULL,
  PRIMARY KEY (`SignupChannel`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table Boderator.Signed
CREATE TABLE IF NOT EXISTS `Signed` (
  `ID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `UserID` bigint(20) unsigned NOT NULL,
  `SlotID` int(10) unsigned NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `SlotLink_idx` (`SlotID`),
  CONSTRAINT `SlotLink` FOREIGN KEY (`SlotID`) REFERENCES `Slots` (`ID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table Boderator.Slots
CREATE TABLE IF NOT EXISTS `Slots` (
  `ID` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) DEFAULT NULL,
  `Emoji` varchar(100) NOT NULL,
  `Count` int(11) DEFAULT NULL,
  `IsReserve` tinyint(4) DEFAULT NULL,
  `TeamID` bigint(20) unsigned NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `TeamLink_idx` (`TeamID`),
  CONSTRAINT `TeamLink` FOREIGN KEY (`TeamID`) REFERENCES `Teams` (`TeamMsg`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

-- Dumping structure for table Boderator.Teams
CREATE TABLE IF NOT EXISTS `Teams` (
  `TeamMsg` bigint(20) unsigned NOT NULL,
  `Name` varchar(100) NOT NULL,
  `Pattern` varchar(1000) NOT NULL,
  `Reserve` bigint(20) unsigned DEFAULT NULL,
  `MissionID` bigint(20) unsigned NOT NULL,
  PRIMARY KEY (`TeamMsg`),
  KEY `MissionID_idx` (`MissionID`),
  CONSTRAINT `MissionLink` FOREIGN KEY (`MissionID`) REFERENCES `Missions` (`SignupChannel`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data exporting was unselected.

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
