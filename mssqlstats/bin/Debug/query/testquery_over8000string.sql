-- 10000 �������i�[����
--use tempdb
--drop table over8000string
--CREATE TABLE over8000string(col1 varchar(max))
--insert into over8000string values(replicate(cast('1234567890' as varchar(max)),1000))
--insert into over8000string values(replicate('1234567890',1000)) -- cast���Ȃ���8000�Ő؂��
-- ���ʂ��m�F����
--select len(col1) as len from over8000string
--len
--30000
--8000

use tempdb; select * from over8000string