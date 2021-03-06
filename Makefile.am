ACLOCAL_AMFLAGS = -I build/m4/cmissync -I build/m4/shamrock

basedirs = build Extras CmisSync.Lib DataSpaceSync.Console DiagnoseTool
FATBUILD_MAKEFILE=Makefile.fatbuild

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) CmisSync

EXTRA_DIST = \
	README.md \
	legal \
	debian \
	packages

DISTCLEANFILES = \
	intltool-extract \
	intltool-merge \
	intltool-update \
	gnome-doc-utils.make

CLEANFILES = -r $(top_builddir)/bin tmpinst packagebuild

MAINTAINERCLEANFILES = \
	aclocal.m4 config.* configure install-sh ltmain* missing Makefile.in

bootstrap:
	autoreconf -f -s -i
	TEMP=$(shell mktemp -du 2>/dev/null || mktemp -d -u -t 'nuget') nuget restore CmisSync/Linux/CmisSync.sln
	mkdir -p bin
	cp packages/NUnit.2.6.4/lib/nunit.framework.dll bin/
	cp packages/Newtonsoft.Json.8.0.3/lib/net45/Newtonsoft.Json.dll bin/
	cp packages/log4net.2.0.5/lib/net45-full/log4net.dll bin/
	cp packages/DBreeze.1.73.2/lib/net45/DBreeze.dll bin/
	cp packages/Moq.4.2.1510.2205/lib/net40/Moq.dll bin/
	cp packages/Toxiproxy.Net.1.2.11/lib/net45/Toxiproxy.Net.dll bin/
	cp packages/RestSharp.105.2.3/lib/net45/RestSharp.dll bin/

install-exec-local:
	$(INSTALL) -m 0755 $(JSON_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(NUNIT_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(MOQ_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(DBREEZE_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(LOG4NET_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(DOTCMIS_PATH) $(DESTDIR)$(pkglibdir)
	$(INSTALL) -m 0755 $(DATASPACE_PATH) $(DESTDIR)$(pkglibdir)

uninstall-local:
	$(RM) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(JSON_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(NUNIT_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(MOQ_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(DBREEZE_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(LOG4NET_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(DOTCMIS_PATH)) \
		$(DESTDIR)$(pkglibdir)/$(notdir $(DATASPACE_PATH))

# make it a .PHONY target, so it's always executed
.PHONY: tmpinst
tmpinst:
	rm -rf tmpinst
	rm -rf packagebuild
	$(MAKE) DESTDIR=$(abs_top_builddir)/tmpinst install
	cp $(FATBUILD_MAKEFILE) tmpinst$(pkglibdir)/Makefile
	$(MAKE) LIBDIR=$(pkglibdir) PACKAGE_TARGET=$(PACKAGE_TARGET) -C tmpinst$(pkglibdir)
	rm tmpinst$(pkglibdir)/Makefile
	mkdir -p packagebuild/SOURCES
	tar cjCf tmpinst packagebuild/SOURCES/dataspacesync.tar.bz2 usr
	rm -rf tmpinst

debian-package: PACKAGE_TARGET := debian-package
debian-package: tmpinst
	make BASEDIR=$(abs_top_builddir)/packaging VERSION=$(VERSION) -C packaging build

apple-package:
	"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" build CmisSync/Mac/CmisSync.sln -c:Release -t:Clean
	"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" build CmisSync/Mac/CmisSync.sln -c:Release
	"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" build DiagnoseTool/Mac/DiagnoseTool/DiagnoseTool.sln -c:Release -t:Clean
	"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" build DiagnoseTool/Mac/DiagnoseTool/DiagnoseTool.sln -c:Release
	(cd build; sh dmg-creator.sh ../bin/DataSpaceSync.app/; cd ..)
	(cd build; sh dmg-creator.sh ../DiagnoseTool/Mac/DiagnoseTool/bin/Release/DiagnoseTool.app/; cd ..)

rpm-package: PACKAGE_TARGET := rpm-package
rpm-package: tmpinst
	rm -f *.rpm
	rpmbuild -bb --define "_topdir $(abs_top_builddir)/packagebuild" --define "_libdir $(libdir)" build/cmissync.spec
	mv `find packagebuild/RPMS -name "*.rpm"` .
	rm -rf packagebuild

none:

package:
	make $(PACKAGE_TARGET)
