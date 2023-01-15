pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                git credentialsId: 'github-acct', url: 'git@github.com:ProgrammingByPermutation/streaming-tools.git', branch: 'rewrite'
            }
        }
        
        stage('Build & Deploy') {
            steps {
				withCredentials([string(credentialsId: 'GITHUB_PERSONAL_ACCESS_TOKEN', variable: 'TOKEN')]) {
					sh """					
						export GH_TOKEN=$TOKEN
						chmod +x go.sh version.sh
						./go.sh 3.0.0.0
					"""
				}
            }
        }
    }
	
	post {
		always {
			cleanWs cleanWhenFailure: true, notFailBuild: true
		}
	}
}
