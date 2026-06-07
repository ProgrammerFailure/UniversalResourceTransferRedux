# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- [API] Refactored core logic to an interface-based model using `IURT_Transmitter` and `IURT_Receiver`.
  - Developer Note: Implementing `PartModules` must now include `KSPFields` named `transmitterID` and `receiverId`. 
  - These fields must remain identical to the `TransmitterID` and `ReceiverId` properties at all times for the solver to function correctly.

## 0.0.1 - 2026-06-05

### Added

- Initial release.
- Implemented transmission and receiving features.
- Implemented solver to pick max efficiency links.
- Implemented optional prioritization of active vessels.
- Added parts:
  - `bpLargeTransmitterDish` (credit: @AniruddhKSP)
    - In game name: 'BP-T1 "Microwaving" Power Transmitter'
    - Basic EMRadiation transmitter dish
    - Consumes EC
    - Currently, very unrealistic stats
  - `bpwr_rx_panel01E`  (credit: @JadeOfMaar)
    - In game name: "BP-SPRP-048 Electric Power Receiver"
    - Basic EMRadiation receiver rectenna
    - Produces EC
    - Has not received a balancing pass yet
- Implemented optional fields:
  - `resourceTypeTags` (`URT_Transmitter`, `URT_Receiver`):
    - Semicolon separated tags.
    - Default tag "EMRadiation".
    - A receiver and transmitter must have at least one tag in common in order to link together.
  - `diffractionConstant` (`URT_Transmitter`):
    - Float value.
    - Default 1.22.
    - Scales the divergence angle of a transmitter's beam.
    - Important for lasers, relativistic charged particle beams, etc.

### Fixed

- Event handlers deregistering receivers/transmitters on vessel unload
- Incorrect caching of inactive `URT_Transmitter` modules
- `transmitterCurrentMaxAmounts` being reset on scene change
