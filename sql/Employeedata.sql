-- Run against the "testdb" database on DESKTOP-5G6NQEK
USE testdb;
GO

IF OBJECT_ID('dbo.Employeedata', 'U') IS NOT NULL
    DROP TABLE dbo.Employeedata;
GO

CREATE TABLE dbo.Employeedata
(
    Id          INT           NOT NULL PRIMARY KEY,
    Name        NVARCHAR(100) NULL,
    Department  NVARCHAR(100) NULL,
    Email       NVARCHAR(200) NULL,
    Address     NVARCHAR(200) NULL,
    JoiningDate DATE          NULL
);
GO

-- JoiningDate stored as DATE; surfaced to clients formatted as MM/DD/YYYY.
INSERT INTO dbo.Employeedata (Id, Name, Department, Email, Address, JoiningDate) VALUES
    (1,  'Dipak',  'Engineering', 'dipak@company.com',  '12 MG Road, Pune',          '2019-03-15'),
    (2,  'Aaraya', 'Design',      'aaraya@company.com', '88 Park St, Kolkata',       '2020-07-01'),
    (3,  'Rohan',  'Engineering', 'rohan@company.com',  '5 Ring Rd, Delhi',          '2018-11-20'),
    (4,  'Priya',  'HR',          'priya@company.com',  '23 Lake View, Bengaluru',   '2021-01-10'),
    (5,  'Sneha',  'Finance',     'sneha@company.com',  '7 Hill Rd, Mumbai',         '2017-06-05'),
    (6,  'Amit',   'Engineering', 'amit@company.com',   '45 Sector 18, Noida',       '2022-09-12'),
    (7,  'Neha',   'Marketing',   'neha@company.com',   '9 Marine Dr, Chennai',      '2019-12-01'),
    (8,  'Vikram', 'Sales',       'vikram@company.com', '3 Banjara Hills, Hyderabad','2020-02-28'),
    (9,  'Pooja',  'HR',          'pooja@company.com',  '67 Salt Lake, Kolkata',     '2021-08-19'),
    (10, 'Karan',  'Finance',     'karan@company.com',  '14 FC Road, Pune',          '2016-04-25');
GO
