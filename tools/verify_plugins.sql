SELECT p."TypeId", p."IsActive"
FROM config."PlugInConfiguration" p
WHERE p."TypeId" IN (
    '0f1e2d3c-4b5a-6978-8c7d-6e5f4a3b2c1d',
    'abfe2440-e765-4f17-a588-bd9ae3799887',
    '4be779c9-e6b6-47f2-bc23-2e71d82a6c1d',
    '00aa4f0e-911d-49fe-8d88-114c7496d383',
    '2bfc9464-4b76-4d76-8ce1-69b712b65e6c',
    'ed2523c1-f66d-4b53-814e-d2fc0c1f46c0',
    '4564ae2b-4819-4155-b5b2-fe2ed0cf7a7f',
    'eb97a8f6-f6bd-460a-bcbe-253bf679361a',
    '4735cc2c-9e5d-457a-92cb-9d765f74fdfb',
    '548a76cc-242c-441c-bc9d-6c22745a2d72',
    '06d18a9e-2919-4c17-9dbc-6e4f7756495c',
    '4b5d0f55-5b26-4447-b9c0-c272e5d0a141',
    '7177533a-f147-407e-97b0-c4d8e1ac1af4',
    'a990270e-b9c6-4445-bba9-56367a90d31d',
    '3684dc79-d81e-4033-ab2c-537334cf0bb6'
)
ORDER BY p."IsActive" DESC, p."TypeId";
