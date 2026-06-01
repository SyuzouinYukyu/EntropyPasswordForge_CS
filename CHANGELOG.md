# Changelog

## v1.0.8

- Merged the former `配分` and `記号` tabs into `記号・配分`.
- Fixed the UI issue where only the symbol slider could appear abnormally tall.
- Unified uppercase, digit, and symbol slider row heights.
- Removed the visible `有効カスタム記号数` display from the UI.
- Reorganized the symbol/distribution tab for Full HD usability.
- Kept auto-copy, include/exclude custom symbols, minimum-use sliders, and full-screen mouse collection behavior.
- Kept the no-keyboard-entropy, no-network, and no-password-logging policies.

## v1.0.7

- Reorganized the right-side password settings area into tabs.
- Fixed slider-area clipping in Full HD layouts.
- Cleaned up fixed-border and draggable-border visual behavior.

## v1.0.6

- Reworked the layout around Full HD 1920x1080 usage.
- Reduced clipping around the character distribution sliders.
- Fixed unnecessary drag hints on fixed borders.

## v1.0.5

- Changed custom-symbol include mode so all valid custom symbols are always included at least once when symbols are enabled.
- Reduced top-area whitespace.
- Improved fixed and draggable boundary handling.

## v1.0.4

- Added uppercase, digit, and symbol minimum-use sliders.
- Added custom-symbol include/exclude modes.
- Added duplicate mouse-event suppression in full-screen collection mode.

## v1.0.3

- Improved UI color handling.
- Added optional auto-copy behavior with generation count fixed to one.
- Preserved clipboard auto-clear behavior.

## v1.0.2

- Hardened full-screen mouse entropy collection behavior.
- Improved custom-symbol normalization and validation.
- Adjusted default length and generation count.

## v1.0.1

- Improved layout visibility.
- Added full-screen mouse entropy collection mode.
- Added initial icon support.

## v1.0.0

- Initial C# / .NET 8 / Windows Forms implementation.
- Implemented OS CSPRNG based password generation with mouse entropy and CPU-jitter mixing.
- Implemented HMAC-SHA512 DRBG flow, rejection sampling, and Fisher-Yates shuffle.
- Established the no-keyboard-entropy, no-network, and no-password-logging policies.
