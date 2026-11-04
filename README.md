# TBC.OpenBanking.Jws

.NET library for signing and verifying HTTP messages with JWS (detached signatures) for TBC Bank Open Banking APIs.

- `src/TBC.OpenBanking.Jws` – core signing/verification library
- `src/TBC.OpenBanking.Jws.Http` – `HttpClient` message handler and certificate locator
- `src/examples` – sample apps (sample certificates only; never use them in production)

## Build

```
dotnet build TBC.OpenBanking.Jws.slnx
```

## Security

Never commit certificates containing private keys, passwords, or other credentials. The
`SampleConsole` example reads its certificate only from the local
`SAMPLE_CERTIFICATE_PATH` environment variable; its optional password is read from
`SAMPLE_CERTIFICATE_PASSWORD`. `ConsoleHttpClientExample` requires
`JWS_EXAMPLE_BASE_URL` to be an explicitly supplied HTTPS endpoint.

The Web API example contains no bank endpoint, redirect URI, or certificate by
default. Provide these only through untracked local configuration or environment
variables: `OpenBankingConnection__BaseUrl`,
`OpenBankingConnection__RedirectUri`,
`OpenBankingConnection__ClientCertificate`, and
`Jws__Options__SigningCertificate`. Both the endpoint and redirect URI must use
HTTPS.
See [SECURITY.md](SECURITY.md).

## License

See [LICENSE.txt](LICENSE.txt).
