# Contact Energy consumption tracker

Simple poller to load daily power consumption stats from [Contact Energy](contact.co.nz) into InfluxDb and Grafana.

# Preview 

<img src="doc_assets\dashboard.png" alt="dashboard screenshot" width="70%" />

# Prerequisites

* Connection with Energy
* InfluxDb
* Grafana 
* Docker

# Docker compose

The easiest way to get going is to opt for `docker-compose` (something along these lines):

```yaml
version: "3"

services:
  influxdb:
    image: influxdb:1.8-alpine
    volumes:
      - ./influxdb:/var/lib/influxdb

  grafana:
    image: grafana/grafana:10.1.5
    depends_on:
      - influxdb
    ports:
      - 3000:3000
    volumes:
      - ./grafana:/var/lib/grafana

  contact-power:
    image: wiseowls/contact-energy-poller
    environment:
      - CONTACT_ContactAPI:UserName= <your login>
      - CONTACT_ContactAPI:Password= <your password>
      - CONTACT_ContactAPI:ContractId= <your contact ID. you will need to check out the portal to fish it out>
      - CONTACT_ContactAPI:ContractLocation= <property address. string is for display purposes only>
    depends_on:
      - influxdb
```

# Set up

Configuration can be done via environment variables or `appSettings.json`.
To run, application requires UserName, Password, ContractId and ContractLocation.
`UserName`, `Password` - account details obtained through onboarding
`ContractId` is a numeric Id which at the moment must be obtained manually through dev tools:

<img src="doc_assets\contract_id.png" alt="obtaining contract id"/>

`ContractLocation` is intended to be an address for grouping and dashboarding but at the moment is not surfaced

# Command line

It is also possible to run this tool from command line and supply a date to pull data for:

```bash
./contact-energy-poller.exe -d 2021-09-01 
./contact-energy-poller.exe --date 2021-09-01
```
