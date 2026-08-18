USE `database_login`;

DROP TABLE IF EXISTS `accounts`;
CREATE TABLE `accounts` (
  `guid` int(11) NOT NULL AUTO_INCREMENT,
  `account` varchar(50) NOT NULL DEFAULT '',
  `password` varchar(40) NOT NULL DEFAULT '',
  `blocked` tinyint(4) NOT NULL DEFAULT 0,
  `security_code` varchar(50) NOT NULL DEFAULT '',
  `golden_channel` int(11) NOT NULL DEFAULT 0,
  `facebook_status` tinyint(4) NOT NULL DEFAULT 0,
  `secured` tinyint(4) NOT NULL DEFAULT 0,
  `email` varchar(100) NOT NULL DEFAULT '',
  PRIMARY KEY (`guid`),
  UNIQUE KEY `idx_account` (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `server_list`;
CREATE TABLE `server_list` (
  `code` int(11) NOT NULL DEFAULT 0,
  `name` varchar(50) NOT NULL DEFAULT 'Modern [S16]',
  `port` int(11) NOT NULL DEFAULT 55902,
  `ip` varchar(50) NOT NULL DEFAULT '127.0.0.1',
  `flag` int(11) NOT NULL DEFAULT 1,
  `online` tinyint(4) NOT NULL DEFAULT 0,
  `default_world` int(11) NOT NULL DEFAULT 0,
  `default_x` int(11) NOT NULL DEFAULT 125,
  `default_y` int(11) NOT NULL DEFAULT 125,
  `type` int(11) NOT NULL DEFAULT 0,
  `visible` tinyint(4) NOT NULL DEFAULT 1,
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `server_list` (`code`, `name`, `port`, `ip`, `flag`, `online`, `default_world`, `default_x`, `default_y`, `type`, `visible`)
VALUES (0, 'Modern [S16]', 55902, '127.0.0.1', 1, 0, 0, 125, 125, 0, 1)
ON DUPLICATE KEY UPDATE `port` = 55902, `ip` = '127.0.0.1';

DROP TABLE IF EXISTS `accounts_status`;
CREATE TABLE `accounts_status` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `server_group` int(11) NOT NULL DEFAULT 0,
  `current_server` int(11) NOT NULL DEFAULT 0,
  `start_server` int(11) NOT NULL DEFAULT 0,
  `dest_server` int(11) NOT NULL DEFAULT 0,
  `dest_world` int(11) NOT NULL DEFAULT 0,
  `dest_x` int(11) NOT NULL DEFAULT 0,
  `dest_y` int(11) NOT NULL DEFAULT 0,
  `warp_time` int(11) NOT NULL DEFAULT 0,
  `warp_auth_1` int(11) NOT NULL DEFAULT 0,
  `warp_auth_2` int(11) NOT NULL DEFAULT 0,
  `warp_auth_3` int(11) NOT NULL DEFAULT 0,
  `warp_auth_4` int(11) NOT NULL DEFAULT 0,
  `last_ip` varchar(50) NOT NULL DEFAULT '',
  `last_mac` varchar(50) NOT NULL DEFAULT '',
  `last_online` int(11) NOT NULL DEFAULT 0,
  `online` tinyint(4) NOT NULL DEFAULT 0,
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  `type` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`account_id`, `server_group`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_security`;
CREATE TABLE `accounts_security` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `account` varchar(50) NOT NULL DEFAULT '',
  `ip` varchar(50) NOT NULL DEFAULT '',
  `mac` varchar(50) NOT NULL DEFAULT '',
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  KEY `idx_acc_sec` (`account_id`, `ip`, `mac`, `disk_serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_allowed`;
CREATE TABLE `accounts_allowed` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `server` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`account_id`, `server`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_validation`;
CREATE TABLE `accounts_validation` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  PRIMARY KEY (`account_id`, `disk_serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_warning`;
CREATE TABLE `accounts_warning` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  `block_date` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`account_id`, `disk_serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_banned`;
CREATE TABLE `accounts_banned` (
  `account_id` int(11) NOT NULL DEFAULT 0,
  `unban_date` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`account_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `master_pc`;
CREATE TABLE `master_pc` (
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  `mac` varchar(50) NOT NULL DEFAULT '',
  PRIMARY KEY (`disk_serial`, `mac`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `item_serial`;
CREATE TABLE `item_serial` (
  `server` int(11) NOT NULL DEFAULT 0,
  `serial` bigint(20) NOT NULL DEFAULT 0,
  `serial_shop` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`server`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `character_notification`;
CREATE TABLE `character_notification` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `server_group` int(11) NOT NULL DEFAULT 0,
  `char_name` varchar(50) NOT NULL DEFAULT '',
  `facebook_id` varchar(50) NOT NULL DEFAULT '',
  `notification_id` int(11) NOT NULL DEFAULT 0,
  `notification_data` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `accounts_disconnect`;
CREATE TABLE `accounts_disconnect` (
  `server` int(11) NOT NULL DEFAULT 0,
  `account_id` int(11) NOT NULL DEFAULT 0,
  `masive` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`server`, `account_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `block_mac`;
CREATE TABLE `block_mac` (
  `mac` varchar(50) NOT NULL DEFAULT '',
  `comment` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`mac`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `block_diskserial`;
CREATE TABLE `block_diskserial` (
  `disk_serial` varchar(50) NOT NULL DEFAULT '',
  `comment` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`disk_serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `block_ip`;
CREATE TABLE `block_ip` (
  `ip` varchar(50) NOT NULL DEFAULT '',
  `comment` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`ip`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;