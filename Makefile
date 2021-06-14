.PHONY: docs deploy-docs

docs:
	mkdocs build -c

deploy-docs:
	mkdocs gh-deploy -cs
