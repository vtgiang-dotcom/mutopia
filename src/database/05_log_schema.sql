USE `database_log`;

DROP TABLE IF EXISTS `log_trade`;
CREATE TABLE `log_trade` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `char_id_1` int(11) NOT NULL DEFAULT 0,
  `char_id_2` int(11) NOT NULL DEFAULT 0,
  `account_id_1` int(11) NOT NULL DEFAULT 0,
  `account_id_2` int(11) NOT NULL DEFAULT 0,
  `ip_1` varchar(50) NOT NULL DEFAULT '',
  `ip_2` varchar(50) NOT NULL DEFAULT '',
  `data` text,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `log_command`;
CREATE TABLE `log_command` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `account_id` int(11) NOT NULL DEFAULT 0,
  `char_id` int(11) NOT NULL DEFAULT 0,
  `command` varchar(255) NOT NULL DEFAULT '',
  `ip` varchar(50) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;