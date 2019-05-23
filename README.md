# Salgu Multiplayer Template
멀티플레이어 게임을 만들기 위해 필요한 기능들을 모아둔 템플릿 프로젝트입니다.  
다음 기능들을 지원합니다.

## Networking
[Unity Networking LLAPI](https://docs.unity3d.com/Manual/UNetUsingTransport.html)를 사용했습니다.  
LLAPI는 2018.3 버전부터 deprecated 되었으므로 이 프로젝트는 2018.2.21 버전까지만 사용 가능합니다.  
자세한 사용법은 데모씬을 참고하세요.

## Steam
[Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks)를 사용했습니다.  
자동으로 스팀 클라이언트를 초기화 하거나, 위의 Networking 기능과 연동하여 스팀 서버를 관리해 줍니다.  
Steam 기능들은 Facepunch.Steamworks를 직접 이용하면 됩니다.  
스팀 없이 돌아가는 스팀서버 전용 어플리케이션을 개발할 수도 있습니다.  
자세한 사용법은 데모씬을 참고하세요.

## Console
간단한 개발자 콘솔을 사용할 수 있습니다.

## Build Manager
하나의 프로젝트로 여러 빌드버전을 관리할 수 있습니다.  
Scene에 Build Manager 오브젝트를 생성하여 Scene 단위로 빌드 설정을 관리하고 빌드합니다.  
Build Manager가 속한 Scene이 초기씬이 됩니다.  

## References
- [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks)
## License
MIT
